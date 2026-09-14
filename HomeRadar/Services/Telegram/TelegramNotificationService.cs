using System.Text;
using System.Text.Json;
using HomeRadar.Services.SsGe.Models;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.Telegram;

public class TelegramNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotificationService> _logger;

    private const int MaxPhotosPerAlbum = 10;

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Telegram");
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_options.BotToken);

    public async Task<bool> SendListingNotificationAsync(
        string chatId, SsGeListing listing, string filterName, CancellationToken ct = default)
    {
        if (!IsConfigured) return false;

        var caption = FormatListingCaption(listing, filterName);
        var imageUrls = GetAllImageUrls(listing);

        if (imageUrls.Count > 1)
            return await SendMediaGroupAsync(chatId, imageUrls, caption, ct);
        if (imageUrls.Count == 1)
            return await SendPhotoDirectAsync(chatId, imageUrls[0], caption, ct);
        return await SendTextAsync(chatId, caption, ct);
    }

    public async Task<bool> SendTestMessageAsync(string chatId, CancellationToken ct = default)
    {
        if (!IsConfigured) return false;
        return await SendTextAsync(chatId, "HomeRadar: Test notification - your Telegram is connected!", ct);
    }

    public async Task<bool> SendMediaGroupAsync(string chatId, List<string> imageUrls, string caption, CancellationToken ct)
    {
        var media = imageUrls.Select((imgUrl, i) => new
        {
            type = "photo",
            media = imgUrl,
            caption = i == 0 ? caption : "",
            parse_mode = i == 0 ? "HTML" : ""
        }).ToArray();

        var payload = new { chat_id = chatId, media };
        var result = await PostWithRetryAsync($"sendMediaGroup", payload, ct);

        if (!result)
        {
            // Fall back to single photo
            return await SendPhotoDirectAsync(chatId, imageUrls[0], caption, ct);
        }
        return true;
    }

    public async Task<bool> SendPhotoDirectAsync(string chatId, string photoUrl, string caption, CancellationToken ct)
    {
        var payload = new { chat_id = chatId, photo = photoUrl, caption, parse_mode = "HTML" };
        var result = await PostWithRetryAsync("sendPhoto", payload, ct);

        if (!result)
        {
            return await SendTextAsync(chatId, caption, ct);
        }
        return true;
    }

    public async Task<bool> SendTextAsync(string chatId, string text, CancellationToken ct)
    {
        var payload = new { chat_id = chatId, text, parse_mode = "HTML", disable_web_page_preview = false };
        return await PostWithRetryAsync("sendMessage", payload, ct);
    }

    private async Task<bool> PostWithRetryAsync(string method, object payload, CancellationToken ct)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{_options.BotToken}/{method}";
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, content, ct);

                if (response.IsSuccessStatusCode)
                    return true;

                var body = await response.Content.ReadAsStringAsync(ct);

                // Handle rate limiting — wait and retry
                if ((int)response.StatusCode == 429 && attempt < maxRetries)
                {
                    var retryAfter = ParseRetryAfter(body);
                    _logger.LogWarning("Telegram rate limited on {Method}, retrying after {Seconds}s (attempt {Attempt}/{Max})",
                        method, retryAfter, attempt, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter + 1), ct);
                    continue;
                }

                _logger.LogWarning("Telegram {Method} failed for chat: {Status} {Body}",
                    method, response.StatusCode, body);
                return false;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Telegram {Method} connection error, retrying (attempt {Attempt}/{Max})",
                    method, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Telegram {Method}", method);
                return false;
            }
        }

        return false;
    }

    private static int ParseRetryAfter(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("parameters", out var p) &&
                p.TryGetProperty("retry_after", out var ra))
                return ra.GetInt32();
        }
        catch { }
        return 5; // default 5 seconds
    }

    // ===== SS.GE formatting =====

    private static string FormatListingCaption(SsGeListing listing, string filterName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{EscapeHtml(filterName)}</b>");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(listing.Title))
            sb.AppendLine(EscapeHtml(listing.Title));

        var priceUsd = listing.Price.PriceUsd ?? 0;
        var priceGel = listing.Price.PriceGeo ?? 0;
        if (priceUsd > 0)
            sb.AppendLine($"<b>${priceUsd:N0}</b> ({priceGel:N0} GEL)");
        else if (priceGel > 0)
            sb.AppendLine($"<b>{priceGel:N0} GEL</b>");

        var unitUsd = listing.Price.UnitPriceUsd ?? 0;
        if (unitUsd > 0)
            sb.AppendLine($"${unitUsd:N0}/m²");

        var details = new List<string>();
        if (listing.TotalArea > 0) details.Add($"{listing.TotalArea} m²");
        if (!string.IsNullOrEmpty(listing.FloorNumber))
            details.Add($"Floor {listing.FloorNumber}{(listing.TotalAmountOfFloor > 0 ? $"/{listing.TotalAmountOfFloor}" : "")}");
        if (listing.NumberOfBedrooms > 0) details.Add($"{listing.NumberOfBedrooms} bed");
        if (listing.Furniture == true) details.Add("Furnished");

        if (details.Count > 0)
            sb.AppendLine(string.Join(" | ", details));

        var address = listing.Address.DisplayAddress;
        if (!string.IsNullOrEmpty(address))
            sb.AppendLine($"📍 {EscapeHtml(address)}");

        sb.AppendLine();
        sb.AppendLine($"<a href=\"{listing.DetailPageUrl}\">View on SS.GE</a>");

        return sb.ToString();
    }

    private static List<string> GetAllImageUrls(SsGeListing listing)
    {
        return listing.AppImages
            .OrderBy(i => i.IsMain ? 0 : 1)
            .ThenBy(i => i.OrderNo)
            .Where(i => !string.IsNullOrEmpty(i.FileName))
            .Select(i => i.FileName!.Replace("_Thumb.", "."))
            .Take(MaxPhotosPerAlbum)
            .ToList();
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
