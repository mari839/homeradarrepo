namespace HomeRadar.Services.MyHome;

public class MyHomeOptions
{
    public const string SectionName = "MyHome";

    public string StatementsApiBaseUrl { get; set; } = "https://api-statements.tnet.ge";
    public string LocationsApiBaseUrl { get; set; } = "https://api-locations.tnet.ge";
    public string WebsiteKey { get; set; } = "myhome";
    public int SyncIntervalMinutes { get; set; } = 1440;
}
