using System.Text.Json;
using HomeRadar.Services.SsGe.Models;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.SsGe;

public class SsGeTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SsGeOptions _options;
    private readonly ILogger<SsGeTokenService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTime _expiresAt = DateTime.MinValue;

    public SsGeTokenService(
        IHttpClientFactory httpClientFactory,
        IOptions<SsGeOptions> options,
        ILogger<SsGeTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is not null && DateTime.UtcNow < _expiresAt)
            return _accessToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_accessToken is not null && DateTime.UtcNow < _expiresAt)
                return _accessToken;

            _logger.LogInformation("Requesting new SS.GE access token");

            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            });

            var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<SsGeTokenResponse>(json)
                ?? throw new InvalidOperationException("Failed to deserialize token response");

            _accessToken = tokenResponse.AccessToken;
            // Refresh 2 minutes before actual expiry
            _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 120);

            _logger.LogInformation("SS.GE token acquired, expires at {ExpiresAt}", _expiresAt);
            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}
