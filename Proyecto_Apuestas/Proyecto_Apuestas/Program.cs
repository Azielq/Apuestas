using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Proyecto_Apuestas.Configuration;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Helpers;
using Proyecto_Apuestas.Services;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext (MySQL)
builder.Services.AddDbContext<apuestasDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// App services (AutoMapper, Auth, etc.)
builder.Services.AddApplicationServices(builder.Configuration);

// Stripe + Products (our own services, not Stripe SDK ProductService)
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddSingleton<IProductService, ProductService>();

// CORS for localhost
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Session (in-memory)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Stripe secret key (fully qualified name to avoid conflicts)
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Payment:Stripe:SecretKey"];

// Initialize configuration helper
ConfigurationHelper.Initialize(builder.Configuration);
ConfigurationHelper.Initialize(app.Services);

// Validate configuration on startup
using (var scope = app.Services.CreateScope())
{
    var validationService = scope.ServiceProvider.GetRequiredService<IStartupValidationService>();
    var diagnosticsService = scope.ServiceProvider.GetRequiredService<IConfigurationDiagnosticsService>();

    var jsonValid = await diagnosticsService.ValidateJsonFilesAsync();
    if (!jsonValid)
    {
        throw new InvalidOperationException("Invalid JSON configuration files detected");
    }

    var isValid = await validationService.ValidateAllConfigurationsAsync();

    if (!isValid)
    {
        var errors = await validationService.GetValidationErrorsAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical("Application startup failed due to configuration errors: {Errors}",
            string.Join(", ", errors));

        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
        }
        else
        {
            logger.LogWarning("Development mode: continuing despite configuration errors");
            var report = await diagnosticsService.GenerateConfigurationReportAsync();
            logger.LogInformation("Configuration diagnostics report:\n{Report}", report);
        }
    }
    else
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("All configuration validations passed successfully");
    }
}

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("ApiPolicy");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = ConfigurationHelper.ApplicationSettingsTyped.Version,
    Environment = ConfigurationHelper.ApplicationSettingsTyped.Environment
}));

// Dev-only endpoints
if (app.Environment.IsDevelopment())
{
    app.MapGet("/config/validate", async (IStartupValidationService validationService) =>
    {
        var errors = await validationService.GetValidationErrorsAsync();
        return Results.Ok(new
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        });
    });

    app.MapGet("/config/diagnostics", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var diagnostics = await diagnosticsService.RunDiagnosticsAsync();
        return Results.Ok(diagnostics);
    });

    app.MapGet("/config/report", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var report = await diagnosticsService.GenerateConfigurationReportAsync();
        return Results.Text(report, "text/plain");
    });

    app.MapGet("/config/overview", () =>
    {
        var overview = new
        {
            Environment = app.Environment.EnvironmentName,
            HasDatabase = !string.IsNullOrEmpty(ConfigurationHelper.GetConnectionString()),
            HasSendGrid = !string.IsNullOrEmpty(ConfigurationHelper.EmailSettingsTyped.SendGrid.ApiKey),
            HasStripe = !string.IsNullOrEmpty(ConfigurationHelper.PaymentSettingsTyped.Stripe.PublicKey),
            Features = new
            {
                LiveBetting = ConfigurationHelper.Features.EnableLiveBetting,
                CashOut = ConfigurationHelper.Features.EnableCashOut,
                Promotions = ConfigurationHelper.Features.EnablePromotions,
                ApiV2 = ConfigurationHelper.Features.EnableApiV2
            },
            BettingLimits = new
            {
                MinimumBet = ConfigurationHelper.ApplicationSettingsTyped.Betting.MinimumBet,
                MaximumBet = ConfigurationHelper.ApplicationSettingsTyped.Betting.MaximumBet,
                DailyLimit = ConfigurationHelper.ApplicationSettingsTyped.Betting.DailyBetLimit
            },
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(overview);
    });
}

app.Run();
