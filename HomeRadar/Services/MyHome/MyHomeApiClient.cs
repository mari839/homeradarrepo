using System.Text.Json;
using HomeRadar.Services.MyHome.Models;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.MyHome;

public class MyHomeApiClient
{
    private readonly HttpClient _statementsClient;
    private readonly HttpClient _locationsClient;
    private readonly ILogger<MyHomeApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public MyHomeApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MyHomeOptions> options,
        ILogger<MyHomeApiClient> logger)
    {
        var opts = options.Value;

        _statementsClient = httpClientFactory.CreateClient("MyHomeStatements");
        _statementsClient.BaseAddress = new Uri(opts.StatementsApiBaseUrl);
        _statementsClient.DefaultRequestHeaders.Add("x-website-key", opts.WebsiteKey);
        _statementsClient.DefaultRequestHeaders.Add("locale", "ka");
        _statementsClient.DefaultRequestHeaders.Referrer = new Uri("https://www.myhome.ge/");
        _statementsClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");

        _locationsClient = httpClientFactory.CreateClient("MyHomeLocations");
        _locationsClient.BaseAddress = new Uri(opts.LocationsApiBaseUrl);
        _locationsClient.DefaultRequestHeaders.Add("x-website-key", opts.WebsiteKey);
        _locationsClient.DefaultRequestHeaders.Add("locale", "ka");
        _locationsClient.DefaultRequestHeaders.Referrer = new Uri("https://www.myhome.ge/");
        _locationsClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");

        _logger = logger;
    }

    public async Task<MyHomeCitiesResponse> GetCitiesAsync(CancellationToken ct = default)
    {
        return await GetAsync<MyHomeCitiesResponse>(_locationsClient, "/v2/cities", ct) ?? new();
    }

    public async Task<MyHomeParametersResponse> GetParametersAsync(CancellationToken ct = default)
    {
        return await GetAsync<MyHomeParametersResponse>(_statementsClient,
            "/v1/statements/statement-parameters?lang=ka&exclude_cities=1", ct) ?? new();
    }

    public async Task<MyHomeSearchResponse> SearchAsync(string queryString, CancellationToken ct = default)
    {
        var url = "/v1/statements?" + queryString;
        return await GetAsync<MyHomeSearchResponse>(_statementsClient, url, ct) ?? new();
    }

    private async Task<T?> GetAsync<T>(HttpClient client, string url, CancellationToken ct)
    {
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "MyHome API request failed (attempt {Attempt}/{Max}), retrying...", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize MyHome response from {Url}", url);
                throw;
            }
        }
        return default;
    }
}
