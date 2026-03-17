using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HomeRadar.Services.SsGe.Models;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.SsGe;

public class SsGeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SsGeTokenService _tokenService;
    private readonly ILogger<SsGeApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public SsGeApiClient(
        IHttpClientFactory httpClientFactory,
        SsGeTokenService tokenService,
        IOptions<SsGeOptions> options,
        ILogger<SsGeApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SsGe");
        _httpClient.BaseAddress = new Uri(options.Value.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("os", "web");
        _httpClient.DefaultRequestHeaders.Add("accept-language", "ka");
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://home.ss.ge/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<List<SsGeCityDto>> GetCitiesAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<SsGeCityDto>>("/v1/RealEstate/visibleCities", ct) ?? [];
    }

    public async Task<List<SsGeDistrictDto>> GetDistrictsAsync(int cityId, CancellationToken ct = default)
    {
        return await GetAsync<List<SsGeDistrictDto>>($"/v1/RealEstate/DistricsWithSubDistricts{cityId}", ct) ?? [];
    }

    public async Task<List<SsGeMunicipalityDto>> GetMunicipalitiesAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<SsGeMunicipalityDto>>("/v1/RealEstate/Municipalities", ct) ?? [];
    }

    public async Task<SsGeSearchResponse> SearchAsync(SsGeSearchRequest request, CancellationToken ct = default)
    {
        return await PostAsync<SsGeSearchResponse>("/v1/RealEstate/LegendSearch", request, ct)
            ?? new SsGeSearchResponse();
    }

    public async Task<int> SearchCountAsync(SsGeSearchRequest request, CancellationToken ct = default)
    {
        var result = await PostAsync<JsonElement>("/v1/RealEstate/LegendSearchCount", request, ct);
        return result.TryGetProperty("applicationCount", out var count) ? count.GetInt32() : 0;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await SetAuthHeaderAsync(request, ct);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
        await SetAuthHeaderAsync(request, ct);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from {Url}. Response: {Json}",
                url, json.Length > 2000 ? json[..2000] + "..." : json);
            throw;
        }
    }

    private async Task SetAuthHeaderAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenService.GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
