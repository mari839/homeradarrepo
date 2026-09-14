using System.Text.Json;
using HomeRadar.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.Telegram;

public class TelegramBotListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramBotListenerService> _logger;

    private int _lastUpdateId;

    public TelegramBotListenerService(
        IServiceProvider serviceProvider,
        IOptions<TelegramOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramBotListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_options.BotToken))
        {
            _logger.LogWarning("Telegram bot token is not configured, bot listener disabled");
            return;
        }

        _logger.LogInformation("Telegram bot listener started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollUpdatesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error polling Telegram updates");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task PollUpdatesAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Telegram");
        var url = $"https://api.telegram.org/bot{_options.BotToken}/getUpdates?offset={_lastUpdateId + 1}&timeout=5";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.GetProperty("ok").GetBoolean()) return;

        var results = doc.RootElement.GetProperty("result");

        foreach (var update in results.EnumerateArray())
        {
            var updateId = update.GetProperty("update_id").GetInt32();
            if (updateId > _lastUpdateId)
                _lastUpdateId = updateId;

            if (!update.TryGetProperty("message", out var message)) continue;
            if (!message.TryGetProperty("text", out var textEl)) continue;

            var text = textEl.GetString();
            if (string.IsNullOrEmpty(text)) continue;

            var chatId = message.GetProperty("chat").GetProperty("id").GetInt64().ToString();
            var chatTitle = GetChatTitle(message);

            // Handle /connect CODE
            if (text.StartsWith("/connect ", StringComparison.OrdinalIgnoreCase))
            {
                var code = text["/connect ".Length..].Trim();
                await HandleConnectAsync(chatId, chatTitle, code, ct);
            }
            // Handle /start CODE (for deep links: t.me/bot?start=CODE)
            else if (text.StartsWith("/start ") && text.Length > "/start ".Length)
            {
                var code = text["/start ".Length..].Trim();
                await HandleConnectAsync(chatId, chatTitle, code, ct);
            }
            // Handle plain /start
            else if (text.Trim() is "/start")
            {
                await SendReplyAsync(chatId,
                    "Welcome to HomeRadar!\n\n" +
                    "To connect a filter, go to your Saved Filters page on HomeRadar and click \"Connect Telegram\" — " +
                    "you'll get a code to send here.",
                    ct);
            }
        }
    }

    private async Task HandleConnectAsync(string chatId, string chatTitle, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            await SendReplyAsync(chatId, "Please provide a code: /connect YOUR_CODE", ct);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check SS.GE filters
        var ssFilter = await db.SavedFilters
            .FirstOrDefaultAsync(f => f.TelegramLinkCode == code, ct);

        if (ssFilter is not null)
        {
            ssFilter.TelegramChatId = chatId;
            ssFilter.TelegramLinkCode = null;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("SS.GE filter {FilterId} '{FilterName}' linked to chat {ChatId}",
                ssFilter.Id, ssFilter.Name, chatId);

            await SendReplyAsync(chatId,
                $"Connected! This chat will receive SS.GE notifications for: \"{ssFilter.Name}\"", ct);
            return;
        }

        // Check MyHome filters
        var myHomeFilter = await db.MyHomeSavedFilters
            .FirstOrDefaultAsync(f => f.TelegramLinkCode == code, ct);

        if (myHomeFilter is not null)
        {
            myHomeFilter.TelegramChatId = chatId;
            myHomeFilter.TelegramLinkCode = null;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("MyHome filter {FilterId} '{FilterName}' linked to chat {ChatId}",
                myHomeFilter.Id, myHomeFilter.Name, chatId);

            await SendReplyAsync(chatId,
                $"Connected! This chat will receive MyHome.ge notifications for: \"{myHomeFilter.Name}\"", ct);
            return;
        }

        await SendReplyAsync(chatId, "Invalid or expired code. Please get a new code from the Saved Filters page.", ct);
    }

    private async Task SendReplyAsync(string chatId, string text, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Telegram");
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
            var payload = new { chat_id = chatId, text };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");
            await client.PostAsync(url, content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reply to chat {ChatId}", chatId);
        }
    }

    private static string GetChatTitle(JsonElement message)
    {
        var chat = message.GetProperty("chat");
        if (chat.TryGetProperty("title", out var title))
            return title.GetString() ?? "";
        if (chat.TryGetProperty("first_name", out var firstName))
            return firstName.GetString() ?? "";
        return "";
    }
}
