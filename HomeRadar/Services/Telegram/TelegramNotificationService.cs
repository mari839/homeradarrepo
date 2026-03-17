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

    // Telegram limits media group to 10 items
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
        if (!IsConfigured)
        {
            _logger.LogWarning("Telegram bot token is not configured");
            return false;
        }

        var caption = FormatListingCaption(listing, filterName);
        var imageUrls = GetAllImageUrls(listing);

        if (imageUrls.Count > 1)
            return await SendMediaGroupAsync(chatId, imageUrls, caption, ct);

        if (imageUrls.Count == 1)
            return await SendPhotoAsync(chatId, imageUrls[0], caption, ct);

        return await SendMessageAsync(chatId, caption, ct);
    }

    public async Task<bool> SendTestMessageAsync(string chatId, CancellationToken ct = default)
    {
        if (!IsConfigured) return false;
        return await SendMessageAsync(chatId, "HomeRadar: Test notification - your Telegram is connected!", ct);
    }

    private async Task<bool> SendMediaGroupAsync(string chatId, List<string> imageUrls, string caption, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMediaGroup";

            var media = imageUrls.Select((imgUrl, i) => new
            {
                type = "photo",
                media = imgUrl,
                // Caption goes on the first photo only
                caption = i == 0 ? caption : "",
                parse_mode = i == 0 ? "HTML" : ""
            }).ToArray();

            var payload = new
            {
                chat_id = chatId,
                media
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Telegram sendMediaGroup failed for chat {ChatId}: {Status} {Body}, falling back to single photo",
                    chatId, response.StatusCode, body);
                // Fall back to single photo with first image
                return await SendPhotoAsync(chatId, imageUrls[0], caption, ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram media group to chat {ChatId}", chatId);
            return await SendPhotoAsync(chatId, imageUrls[0], caption, ct);
        }
    }

    private async Task<bool> SendPhotoAsync(string chatId, string photoUrl, string caption, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendPhoto";
            var payload = new
            {
                chat_id = chatId,
                photo = photoUrl,
                caption,
                parse_mode = "HTML"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Telegram sendPhoto failed for chat {ChatId}: {Status} {Body}, falling back to text",
                    chatId, response.StatusCode, body);
                return await SendMessageAsync(chatId, caption, ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram photo to chat {ChatId}", chatId);
            return await SendMessageAsync(chatId, caption, ct);
        }
    }

    private async Task<bool> SendMessageAsync(string chatId, string text, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text,
                parse_mode = "HTML",
                disable_web_page_preview = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Telegram API error for chat {ChatId}: {Status} {Body}",
                    chatId, response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message to chat {ChatId}", chatId);
            return false;
        }
    }

    private static string FormatListingCaption(SsGeListing listing, string filterName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{EscapeHtml(filterName)}</b>");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(listing.Title))
            sb.AppendLine(EscapeHtml(listing.Title));

        // Price
        var priceUsd = listing.Price.PriceUsd ?? 0;
        var priceGel = listing.Price.PriceGeo ?? 0;
        if (priceUsd > 0)
            sb.AppendLine($"<b>${priceUsd:N0}</b> ({priceGel:N0} GEL)");
        else if (priceGel > 0)
            sb.AppendLine($"<b>{priceGel:N0} GEL</b>");

        // Per sqm
        var unitUsd = listing.Price.UnitPriceUsd ?? 0;
        if (unitUsd > 0)
            sb.AppendLine($"${unitUsd:N0}/m²");

        // Details line
        var details = new List<string>();
        if (listing.TotalArea > 0) details.Add($"{listing.TotalArea} m²");
        if (!string.IsNullOrEmpty(listing.FloorNumber))
            details.Add($"Floor {listing.FloorNumber}{(listing.TotalAmountOfFloor > 0 ? $"/{listing.TotalAmountOfFloor}" : "")}");
        if (listing.NumberOfBedrooms > 0) details.Add($"{listing.NumberOfBedrooms} bed");
        if (listing.Furniture == true) details.Add("Furnished");

        if (details.Count > 0)
            sb.AppendLine(string.Join(" | ", details));

        // Address
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
            .OrderBy(i => i.IsMain ? 0 : 1) // main image first
            .ThenBy(i => i.OrderNo)
            .Where(i => !string.IsNullOrEmpty(i.FileName))
            .Select(i => i.FileName!.Replace("_Thumb.", ".")) // full size
            .Take(MaxPhotosPerAlbum)
            .ToList();
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
