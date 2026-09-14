using System.Text.Json;
using HomeRadar.Data;
using HomeRadar.Services.SsGe;
using HomeRadar.Services.SsGe.Models;
using HomeRadar.Services.Telegram;
using Microsoft.EntityFrameworkCore;

namespace HomeRadar.Services.Monitoring;

public class ListingMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramNotificationService _telegram;
    private readonly ILogger<ListingMonitorService> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DelayBetweenFilters = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan DelayBetweenNotifications = TimeSpan.FromSeconds(5);

    // Track recently notified listing IDs per filter to prevent duplicates
    private readonly Dictionary<int, HashSet<int>> _notifiedIds = new();

    public ListingMonitorService(
        IServiceProvider serviceProvider,
        TelegramNotificationService telegram,
        ILogger<ListingMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _telegram = telegram;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllFiltersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in listing monitor cycle");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task CheckAllFiltersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var apiClient = scope.ServiceProvider.GetRequiredService<SsGeApiClient>();

        var activeFilters = await db.SavedFilters
            .Where(f => f.IsActive)
            .ToListAsync(ct);

        if (activeFilters.Count == 0) return;

        _logger.LogInformation("Checking {Count} active filters for new listings", activeFilters.Count);

        foreach (var filter in activeFilters)
        {
            try
            {
                await CheckFilterAsync(filter, apiClient, ct);
                await Task.Delay(DelayBetweenFilters, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking filter {FilterId} '{FilterName}'", filter.Id, filter.Name);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task CheckFilterAsync(
        Data.Entities.SavedFilter filter,
        SsGeApiClient apiClient,
        CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<SsGeSearchRequest>(filter.SearchRequestJson);
        if (request is null)
        {
            _logger.LogWarning("Filter {FilterId} has invalid JSON, skipping", filter.Id);
            return;
        }

        // Fetch newest listings — order=1 bypasses VIP on SS.GE
        request.Order = 1; // DateDesc (newest first)
        request.Page = 1;
        request.PageSize = 16;

        var response = await apiClient.SearchAsync(request, ct);
        if (response.Listings.Count == 0)
        {
            filter.LastCheckedAt = DateTime.UtcNow;
            return;
        }

        // Initialize notified set for this filter if needed
        if (!_notifiedIds.TryGetValue(filter.Id, out var notified))
        {
            notified = [];
            _notifiedIds[filter.Id] = notified;
        }

        // Very first run — just record baseline, no notifications
        if (filter.LastSeenApplicationId is null)
        {
            foreach (var listing in response.Listings)
                notified.Add(listing.ApplicationId);

            filter.LastSeenApplicationId = response.Listings.Max(l => l.ApplicationId);
            filter.LastCheckedAt = DateTime.UtcNow;

            _logger.LogInformation("Filter {FilterId} '{FilterName}': first run, baseline recorded {Count} listings, watermark={MaxId}",
                filter.Id, filter.Name, response.Listings.Count, filter.LastSeenApplicationId);
            return;
        }

        // App restart — populate notified set with known old listings, but let new ones through
        if (notified.Count == 0)
        {
            foreach (var listing in response.Listings.Where(l => l.ApplicationId <= filter.LastSeenApplicationId))
                notified.Add(listing.ApplicationId);

            _logger.LogInformation("Filter {FilterId} '{FilterName}': restart, restored {Count} known IDs, watermark={MaxId}",
                filter.Id, filter.Name, notified.Count, filter.LastSeenApplicationId);
            // Don't return — fall through to normal detection below
        }

        // Find listings we haven't notified about yet
        var newListings = response.Listings
            .Where(l => !notified.Contains(l.ApplicationId)
                        && l.ApplicationId > filter.LastSeenApplicationId)
            .OrderBy(l => l.ApplicationId)
            .ToList();

        if (newListings.Count == 0)
        {
            filter.LastCheckedAt = DateTime.UtcNow;
            return;
        }

        _logger.LogInformation("Filter {FilterId} '{FilterName}': {Count} new listings found",
            filter.Id, filter.Name, newListings.Count);

        // Send Telegram notifications
        if (!string.IsNullOrEmpty(filter.TelegramChatId) && _telegram.IsConfigured)
        {
            foreach (var listing in newListings)
            {
                await _telegram.SendListingNotificationAsync(filter.TelegramChatId, listing, filter.Name, ct);
                notified.Add(listing.ApplicationId);
                await Task.Delay(DelayBetweenNotifications, ct);
            }
        }
        else
        {
            // Still track as notified even without Telegram
            foreach (var listing in newListings)
                notified.Add(listing.ApplicationId);
        }

        // Update the high watermark
        var maxId = response.Listings.Max(l => l.ApplicationId);
        if (maxId > filter.LastSeenApplicationId)
            filter.LastSeenApplicationId = maxId;

        filter.LastCheckedAt = DateTime.UtcNow;

        // Keep notified set from growing unbounded — only keep IDs above the watermark
        var watermark = filter.LastSeenApplicationId.Value;
        notified.RemoveWhere(id => id < watermark - 1000);
    }
}
