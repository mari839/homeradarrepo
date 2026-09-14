using HomeRadar.Data;
using HomeRadar.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.MyHome;

public class MyHomeReferenceSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MyHomeOptions _options;
    private readonly ILogger<MyHomeReferenceSyncService> _logger;

    public MyHomeReferenceSyncService(
        IServiceProvider serviceProvider,
        IOptions<MyHomeOptions> options,
        ILogger<MyHomeReferenceSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync MyHome reference data");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.SyncIntervalMinutes), stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting MyHome reference data sync");

        using var scope = _serviceProvider.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<MyHomeApiClient>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var citiesResponse = await apiClient.GetCitiesAsync(ct);

        var cities = new List<MyHomeCity>();
        var districts = new List<MyHomeDistrict>();
        var urbans = new List<MyHomeUrban>();

        foreach (var city in citiesResponse.Data)
        {
            cities.Add(new MyHomeCity { Id = city.Id, Title = city.DisplayName });

            if (city.Districts is null) continue;
            foreach (var district in city.Districts)
            {
                districts.Add(new MyHomeDistrict
                {
                    Id = district.Id,
                    Title = district.DisplayName,
                    CityId = city.Id
                });

                if (district.Urbans is null) continue;
                foreach (var urban in district.Urbans)
                {
                    urbans.Add(new MyHomeUrban
                    {
                        Id = urban.Id,
                        Title = urban.DisplayName,
                        DistrictId = district.Id
                    });
                }
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.MyHomeUrbans.ExecuteDeleteAsync(ct);
            await db.MyHomeDistricts.ExecuteDeleteAsync(ct);
            await db.MyHomeCities.ExecuteDeleteAsync(ct);

            db.MyHomeCities.AddRange(cities);
            db.MyHomeDistricts.AddRange(districts);
            db.MyHomeUrbans.AddRange(urbans);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "MyHome reference data synced: {Cities} cities, {Districts} districts, {Urbans} urbans",
                cities.Count, districts.Count, urbans.Count);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
