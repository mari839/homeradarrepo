using HomeRadar.Data;
using HomeRadar.Services.MyHome.Models;
using HomeRadar.Services.Telegram;
using Microsoft.EntityFrameworkCore;

namespace HomeRadar.Services.MyHome;

public class MyHomeMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramNotificationService _telegram;
    private readonly ILogger<MyHomeMonitorService> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DelayBetweenFilters = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan DelayBetweenNotifications = TimeSpan.FromSeconds(5);

    // Track notified listing IDs per filter to prevent duplicates
    private readonly Dictionary<int, HashSet<int>> _notifiedIds = new();

    public MyHomeMonitorService(
        IServiceProvider serviceProvider,
        TelegramNotificationService telegram,
        ILogger<MyHomeMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _telegram = telegram;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(35), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllFiltersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MyHome listing monitor cycle");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task CheckAllFiltersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var apiClient = scope.ServiceProvider.GetRequiredService<MyHomeApiClient>();

        var activeFilters = await db.MyHomeSavedFilters
            .Where(f => f.IsActive)
            .ToListAsync(ct);

        if (activeFilters.Count == 0) return;

        _logger.LogInformation("Checking {Count} active MyHome filters", activeFilters.Count);

        foreach (var filter in activeFilters)
        {
            try
            {
                await CheckFilterAsync(filter, apiClient, ct);
                await Task.Delay(DelayBetweenFilters, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MyHome filter {FilterId} '{FilterName}'", filter.Id, filter.Name);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task CheckFilterAsync(
        Data.Entities.MyHomeSavedFilter filter,
        MyHomeApiClient apiClient,
        CancellationToken ct)
    {
        // Sort by date desc and fetch enough listings to get past VIP-dominated results
        var qs = filter.SearchQueryString;
        if (!qs.Contains("order_by="))
            qs += "&order_by=date&sequence=desc";
        // Remove existing page/per_page params and set our own
        qs = System.Text.RegularExpressions.Regex.Replace(qs, @"[&?]?per_page=\d+", "");
        qs = System.Text.RegularExpressions.Regex.Replace(qs, @"[&?]?(?<!per_)page=\d+", "");
        qs = qs.Trim('&') + $"&page=1&per_page={filter.PerPage}";

        var response = await apiClient.SearchAsync(qs, ct);
        var listings = response.Data.Listings;

        // API doesn't reliably filter VIP listings by price — apply local filters
        var originalQs = System.Web.HttpUtility.ParseQueryString(filter.SearchQueryString);
        listings = ApplyLocalFilters(listings, originalQs);

        if (listings.Count == 0)
        {
            filter.LastCheckedAt = DateTime.UtcNow;
            return;
        }

        if (!_notifiedIds.TryGetValue(filter.Id, out var notified))
        {
            notified = [];
            _notifiedIds[filter.Id] = notified;
        }

        // Very first run — just record baseline, no notifications
        if (filter.LastSeenUpdatedAt is null)
        {
            foreach (var listing in listings)
                notified.Add(listing.Id);

            filter.LastSeenListingId = listings.Max(l => l.Id);
            filter.LastSeenUpdatedAt = listings
                .Where(l => !string.IsNullOrEmpty(l.LastUpdated))
                .Max(l => l.LastUpdated);
            filter.LastCheckedAt = DateTime.UtcNow;

            _logger.LogInformation("MyHome filter {FilterId} '{FilterName}': first run, baseline recorded {Count} listings",
                filter.Id, filter.Name, listings.Count);
            return;
        }

        // App restart — populate notified set with known old listings, but let new ones through
        if (notified.Count == 0)
        {
            var savedId = filter.LastSeenListingId ?? 0;
            var savedUpd = filter.LastSeenUpdatedAt ?? "";

            foreach (var listing in listings.Where(l =>
                l.Id <= savedId &&
                (string.IsNullOrEmpty(l.LastUpdated) || string.Compare(l.LastUpdated, savedUpd, StringComparison.Ordinal) <= 0)))
            {
                notified.Add(listing.Id);
            }

            _logger.LogInformation("MyHome filter {FilterId} '{FilterName}': restart, restored {Count} known IDs",
                filter.Id, filter.Name, notified.Count);
            // Don't return — fall through to normal detection below
        }

        // Find new listings: either new ID or updated since our last check
        var lastUpdated = filter.LastSeenUpdatedAt ?? "";
        var lastId = filter.LastSeenListingId ?? 0;

        _logger.LogInformation(
            "MyHome filter {FilterId} '{FilterName}': fetched {Count} listings (query: {Query}), lastId={LastId}, lastUpdated={LastUpdated}",
            filter.Id, filter.Name, listings.Count, qs, lastId, lastUpdated);

        var newListings = listings
            .Where(l => !notified.Contains(l.Id) &&
                        (l.Id > lastId ||
                         (!string.IsNullOrEmpty(l.LastUpdated) && string.Compare(l.LastUpdated, lastUpdated, StringComparison.Ordinal) > 0)))
            .OrderBy(l => l.LastUpdated)
            .ToList();

        if (newListings.Count == 0)
        {
            filter.LastCheckedAt = DateTime.UtcNow;
            return;
        }

        _logger.LogInformation("MyHome filter {FilterId} '{FilterName}': {Count} new/updated listings to notify",
            filter.Id, filter.Name, newListings.Count);

        // Send notifications
        if (!string.IsNullOrEmpty(filter.TelegramChatId) && _telegram.IsConfigured)
        {
            foreach (var listing in newListings)
            {
                await SendMyHomeNotificationAsync(filter.TelegramChatId, listing, filter.Name, ct);
                notified.Add(listing.Id);
                await Task.Delay(DelayBetweenNotifications, ct);
            }
        }
        else
        {
            foreach (var listing in newListings)
                notified.Add(listing.Id);
        }

        // Update watermarks
        var maxId = listings.Max(l => l.Id);
        if (maxId > lastId)
            filter.LastSeenListingId = maxId;

        var maxUpdated = listings
            .Where(l => !string.IsNullOrEmpty(l.LastUpdated))
            .Max(l => l.LastUpdated);
        if (!string.IsNullOrEmpty(maxUpdated) && string.Compare(maxUpdated, lastUpdated, StringComparison.Ordinal) > 0)
            filter.LastSeenUpdatedAt = maxUpdated;

        filter.LastCheckedAt = DateTime.UtcNow;

        // Prune notified set
        notified.RemoveWhere(id => id < (filter.LastSeenListingId ?? 0) - 1000);
    }

    private async Task SendMyHomeNotificationAsync(
        string chatId, MyHomeListing listing, string filterName, CancellationToken ct)
    {
        var imageUrls = listing.Images?
            .OrderBy(i => i.IsMain ? 0 : 1)
            .Where(i => !string.IsNullOrEmpty(i.Large))
            .Select(i => i.Large!)
            .Take(10)
            .ToList() ?? [];

        var caption = FormatCaption(listing, filterName);

        if (imageUrls.Count > 1)
            await _telegram.SendMediaGroupAsync(chatId, imageUrls, caption, ct);
        else if (imageUrls.Count == 1)
            await _telegram.SendPhotoDirectAsync(chatId, imageUrls[0], caption, ct);
        else
            await _telegram.SendTextAsync(chatId, caption, ct);
    }

    private static string FormatCaption(MyHomeListing listing, string filterName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>🏠 MyHome: {EscapeHtml(filterName)}</b>");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(listing.DynamicTitle))
            sb.AppendLine(EscapeHtml(listing.DynamicTitle));

        if (listing.PriceUsd > 0)
            sb.AppendLine($"<b>${listing.PriceUsd:N0}</b> ({listing.PriceGel:N0} GEL)");
        else if (listing.PriceGel > 0)
            sb.AppendLine($"<b>{listing.PriceGel:N0} GEL</b>");

        if (listing.UnitPriceUsd > 0)
            sb.AppendLine($"${listing.UnitPriceUsd:N0}/m²");

        var details = new List<string>();
        if (listing.Area > 0) details.Add($"{listing.Area} m²");
        if (listing.Floor > 0) details.Add($"Floor {listing.Floor}{(listing.TotalFloors > 0 ? $"/{listing.TotalFloors}" : "")}");
        if (!string.IsNullOrEmpty(listing.Room)) details.Add($"{listing.Room} rooms");
        if (!string.IsNullOrEmpty(listing.Bedroom)) details.Add($"{listing.Bedroom} bed");

        if (details.Count > 0)
            sb.AppendLine(string.Join(" | ", details));

        var addr = listing.DisplayAddress;
        if (!string.IsNullOrEmpty(addr))
            sb.AppendLine($"📍 {EscapeHtml(addr)}");

        sb.AppendLine();
        sb.AppendLine($"<a href=\"{listing.DetailPageUrl}\">View on MyHome.ge</a>");

        return sb.ToString();
    }

    private static List<MyHomeListing> ApplyLocalFilters(
        List<MyHomeListing> listings, System.Collections.Specialized.NameValueCollection qs)
    {
        var currencyKey = qs["currency_id"] ?? "2"; // default USD

        if (decimal.TryParse(qs["price_from"], out var priceFrom))
            listings = listings.Where(l => GetPrice(l, currencyKey) >= priceFrom).ToList();

        if (decimal.TryParse(qs["price_to"], out var priceTo))
            listings = listings.Where(l => GetPrice(l, currencyKey) <= priceTo).ToList();

        if (decimal.TryParse(qs["square_price_from"], out var sqFrom))
            listings = listings.Where(l => GetSquarePrice(l, currencyKey) >= sqFrom).ToList();

        if (decimal.TryParse(qs["square_price_to"], out var sqTo))
            listings = listings.Where(l => GetSquarePrice(l, currencyKey) <= sqTo).ToList();

        if (double.TryParse(qs["area_from"], out var areaFrom))
            listings = listings.Where(l => (l.Area ?? 0) >= areaFrom).ToList();

        if (double.TryParse(qs["area_to"], out var areaTo))
            listings = listings.Where(l => (l.Area ?? 0) <= areaTo).ToList();

        if (int.TryParse(qs["floor_from"], out var floorFrom))
            listings = listings.Where(l => (l.Floor ?? 0) >= floorFrom).ToList();

        if (int.TryParse(qs["floor_to"], out var floorTo))
            listings = listings.Where(l => (l.Floor ?? 0) <= floorTo).ToList();

        if (qs["not_first"] == "1")
            listings = listings.Where(l => (l.Floor ?? 0) > 1).ToList();

        return listings;
    }

    private static decimal GetPrice(MyHomeListing l, string currencyKey)
        => l.Price?.TryGetValue(currencyKey, out var p) == true ? p.PriceTotal : 0;

    private static decimal GetSquarePrice(MyHomeListing l, string currencyKey)
        => l.Price?.TryGetValue(currencyKey, out var p) == true ? p.PriceSquare : 0;

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
