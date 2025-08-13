using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Proyecto_Apuestas.Configuration;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Helpers;
using Proyecto_Apuestas.Services;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// IMPORTANT: Esto es lo que se Agrega para Entity Framework con MySQL para su correcto funcionamiento
builder.Services.AddDbContext<apuestasDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Register application services, AutoMapper, Authentication, Authorization, and Session
builder.Services.AddApplicationServices(builder.Configuration);

// Stripe Service
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddSingleton<IProductService, ProductService>();

var app = builder.Build();

// Initialize configuration helper with both configuration and service provider
ConfigurationHelper.Initialize(builder.Configuration);
ConfigurationHelper.Initialize(app.Services);

// Validate configuration on startup
using (var scope = app.Services.CreateScope())
{
    var validationService = scope.ServiceProvider.GetRequiredService<IStartupValidationService>();
    var diagnosticsService = scope.ServiceProvider.GetRequiredService<IConfigurationDiagnosticsService>();
    
    // Validate JSON files first
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
            logger.LogWarning("Development mode: Continuing despite configuration errors");
            
            // Generate diagnostics report in development
            var report = await diagnosticsService.GenerateConfigurationReportAsync();
            logger.LogInformation("Configuration Diagnostics Report:\n{Report}", report);
        }
    }
    else
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("? All configuration validations passed successfully");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// NOTE: Esto es para servir archivos estáticos desde la carpeta wwwroot
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseRouting();

// Add CORS before authentication
app.UseCors("ApiPolicy");

// Add session middleware before authentication
app.UseSession();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = ConfigurationHelper.ApplicationSettingsTyped.Version,
    Environment = ConfigurationHelper.ApplicationSettingsTyped.Environment
}));

// Development-only endpoints
if (app.Environment.IsDevelopment())
{
    // Configuration validation endpoint
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

    // Configuration diagnostics endpoint
    app.MapGet("/config/diagnostics", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var diagnostics = await diagnosticsService.RunDiagnosticsAsync();
        return Results.Ok(diagnostics);
    });

    // Configuration report endpoint
    app.MapGet("/config/report", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var report = await diagnosticsService.GenerateConfigurationReportAsync();
        return Results.Text(report, "text/plain");
    });

    // Configuration settings overview (non-sensitive data only)
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

