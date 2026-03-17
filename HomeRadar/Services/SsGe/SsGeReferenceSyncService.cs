using HomeRadar.Data;
using HomeRadar.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeRadar.Services.SsGe;

public class SsGeReferenceSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SsGeOptions _options;
    private readonly ILogger<SsGeReferenceSyncService> _logger;

    public SsGeReferenceSyncService(
        IServiceProvider serviceProvider,
        IOptions<SsGeOptions> options,
        ILogger<SsGeReferenceSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the app to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncReferenceDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync SS.GE reference data");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.SyncIntervalMinutes), stoppingToken);
        }
    }

    private async Task SyncReferenceDataAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting SS.GE reference data sync");

        using var scope = _serviceProvider.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<SsGeApiClient>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Fetch all data from SS.GE API
        var cities = await apiClient.GetCitiesAsync(ct);
        var municipalities = await apiClient.GetMunicipalitiesAsync(ct);

        var allDistricts = new List<District>();
        var allSubDistricts = new List<SubDistrict>();

        foreach (var city in cities)
        {
            var districts = await apiClient.GetDistrictsAsync(city.Id, ct);
            foreach (var district in districts)
            {
                allDistricts.Add(new District
                {
                    Id = district.Id,
                    Title = district.Title,
                    CityId = city.Id
                });

                foreach (var sub in district.SubDistricts)
                {
                    allSubDistricts.Add(new SubDistrict
                    {
                        Id = sub.Id,
                        Title = sub.Title,
                        DistrictId = district.Id
                    });
                }
            }
        }

        // Replace all reference data in a transaction
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SubDistricts.ExecuteDeleteAsync(ct);
            await db.Districts.ExecuteDeleteAsync(ct);
            await db.Cities.ExecuteDeleteAsync(ct);
            await db.Municipalities.ExecuteDeleteAsync(ct);

            db.Cities.AddRange(cities.Select(c => new City
            {
                Id = c.Id,
                Order = c.Order,
                Title = c.Title
            }));

            db.Municipalities.AddRange(municipalities.Select(m => new Municipality
            {
                Id = m.Id,
                Order = m.Order,
                Title = m.Title
            }));

            db.Districts.AddRange(allDistricts);
            db.SubDistricts.AddRange(allSubDistricts);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "SS.GE reference data synced: {Cities} cities, {Districts} districts, {SubDistricts} subdistricts, {Municipalities} municipalities",
                cities.Count, allDistricts.Count, allSubDistricts.Count, municipalities.Count);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
