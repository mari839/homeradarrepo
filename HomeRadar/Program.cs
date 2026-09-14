using HomeRadar.Components;
using HomeRadar.Components.Account;
using HomeRadar.Data;
using HomeRadar.Services.Monitoring;
using HomeRadar.Services.MyHome;
using HomeRadar.Services.SsGe;
using HomeRadar.Services.Telegram;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// SS.GE integration
builder.Services.Configure<SsGeOptions>(builder.Configuration.GetSection(SsGeOptions.SectionName));
builder.Services.AddHttpClient("SsGe");
builder.Services.AddSingleton<SsGeTokenService>();
builder.Services.AddScoped<SsGeApiClient>();
builder.Services.AddHostedService<SsGeReferenceSyncService>();

// Telegram notifications
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.AddHttpClient("Telegram")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    });
builder.Services.AddSingleton<TelegramNotificationService>();
builder.Services.AddHostedService<TelegramBotListenerService>();

// Listing monitor (SS.GE)
builder.Services.AddHostedService<ListingMonitorService>();

// MyHome.ge integration
builder.Services.Configure<MyHomeOptions>(builder.Configuration.GetSection(MyHomeOptions.SectionName));
builder.Services.AddHttpClient("MyHomeStatements")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    });
builder.Services.AddHttpClient("MyHomeLocations");
builder.Services.AddScoped<MyHomeApiClient>();
builder.Services.AddHostedService<MyHomeReferenceSyncService>();
builder.Services.AddHostedService<MyHomeMonitorService>();

var app = builder.Build();

// Apply any pending EF Core migrations on startup (creates the SQLite file on first run).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
