namespace HomeRadar.Services.SsGe;

public class SsGeOptions
{
    public const string SectionName = "SsGe";

    public string TokenEndpoint { get; set; } = "https://account.ss.ge/connect/token";
    public string ApiBaseUrl { get; set; } = "https://api-gateway.ss.ge";
    public string ClientId { get; set; } = "ssweb";
    public string ClientSecret { get; set; } = "t5w42KQQjowNRYkycrrX";
    public int SyncIntervalMinutes { get; set; } = 1440;
}
