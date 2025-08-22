using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Configuration;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Helpers;
using Proyecto_Apuestas.Services;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Services.Interfaces;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Configure Serilog early
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var webHostEnvironment = new SimpleWebHostEnvironment { EnvironmentName = environment };

// Configure Serilog
Log.Logger = LoggingConfiguration.ConfigureSerilog(configuration, webHostEnvironment).CreateLogger();

try
{
    Log.Information("🚀 Starting Proyecto Apuestas application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as the logging provider
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // IMPORTANT: Esto es lo que se Agrega para Entity Framework con MySQL para su correcto funcionamiento
    builder.Services.AddDbContext<apuestasDbContext>(options =>
        options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

    // Register application services, AutoMapper, Authentication, Authorization, and Session
    builder.Services.AddApplicationServices(builder.Configuration);

    // Configure HTTP client for OddsApiService
    builder.Services.AddHttpClient<IOddsApiService, OddsApiService>(client =>
    {
        var baseUrl = builder.Configuration["OddsApi:BaseUrl"] ?? "https://api.the-odds-api.com/v4";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "ProyectoApuestas/1.0");
    });

    // Stripe + Products (our own services, not Stripe SDK ProductService)
    builder.Services.AddScoped<IStripeService, StripeService>();
    builder.Services.AddSingleton<IProductService, ProductService>();

    // Add enhanced health checks
    builder.Services.AddEnhancedHealthChecks(builder.Configuration);

    // Add Scalar API documentation
    builder.Services.AddScalarDocumentation(builder.Configuration);

    builder.Services.AddScoped<IOddsService, OddsService>();

    // Cultura por defecto es-CR para mostrar Colones correctamente en {monto:C}
    var culture = new System.Globalization.CultureInfo("es-CR");
    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

    var app = builder.Build();

    // Configure Serilog request logging
    app.UseSerilogRequestLogging(LoggingConfiguration.ConfigureRequestLogging);

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
            Log.Fatal("❌ Invalid JSON configuration files detected");
            throw new InvalidOperationException("Invalid JSON configuration files detected");
        }

        var isValid = await validationService.ValidateAllConfigurationsAsync();

        if (!isValid)
        {
            var errors = await validationService.GetValidationErrorsAsync();
            Log.Fatal("❌ Application startup failed due to configuration errors: {Errors}", string.Join(", ", errors));

            if (!app.Environment.IsDevelopment())
            {
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
            }
            else
            {
                Log.Warning("⚠️ Development mode: Continuing despite configuration errors");

                // Generate diagnostics report in development
                var report = await diagnosticsService.GenerateConfigurationReportAsync();
                Log.Information("📋 Configuration Diagnostics Report:\n{Report}", report);
            }
        }
        else
        {
            Log.Information("✅ All configuration validations passed successfully");
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

    // IMPORTANT: Add session middleware BEFORE Serilog request logging
    // This ensures session is available when logging tries to access it
    app.UseSession();

    // Add authentication and authorization AFTER session
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    // Enhanced Health Check Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Readiness probe for Kubernetes
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("critical"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Liveness probe for Kubernetes
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // Only basic liveness check
        ResponseWriter = (context, result) =>
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync($$"""{"status":"{{result.Status}}","timestamp":"{{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}}"}""");
        }
    });

    // Database health check
    app.MapHealthChecks("/health/database", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("database"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // External services health check
    app.MapHealthChecks("/health/external", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("external"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // System resources health check
    app.MapHealthChecks("/health/system", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("system"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Health Checks UI
    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/healthchecks-ui";
        options.ApiPath = "/healthchecks-ui-api";
    });

    // Use Scalar API documentation
    app.UseScalarDocumentation(app.Environment);

    // Legacy health endpoint for backward compatibility
    app.MapGet("/health/legacy", () => Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = ConfigurationHelper.ApplicationSettingsTyped?.Version ?? "1.0.0",
        Environment = ConfigurationHelper.ApplicationSettingsTyped?.Environment ?? app.Environment.EnvironmentName
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
            try
            {
                var overview = new
                {
                    Environment = app.Environment.EnvironmentName,
                    HasDatabase = !string.IsNullOrEmpty(ConfigurationHelper.GetConnectionString()),
                    HasSendGrid = !string.IsNullOrEmpty(ConfigurationHelper.EmailSettingsTyped?.SendGrid?.ApiKey),
                    HasStripe = !string.IsNullOrEmpty(ConfigurationHelper.PaymentSettingsTyped?.Stripe?.PublicKey),
                    Features = new
                    {
                        LiveBetting = true, // Default values for safety
                        CashOut = true,
                        Promotions = true,
                        ApiV2 = true
                    },
                    BettingLimits = new
                    {
                        MinimumBet = ConfigurationHelper.ApplicationSettingsTyped?.Betting?.MinimumBet ?? 100,
                        MaximumBet = ConfigurationHelper.ApplicationSettingsTyped?.Betting?.MaximumBet ?? 10000,
                        DailyLimit = ConfigurationHelper.ApplicationSettingsTyped?.Betting?.DailyBetLimit ?? 50000
                    },
                    Timestamp = DateTime.UtcNow
                };

                return Results.Ok(overview);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating configuration overview");
                return Results.Ok(new
                {
                    Environment = app.Environment.EnvironmentName,
                    Error = "Could not load configuration details",
                    Timestamp = DateTime.UtcNow
                });
            }
        });

        Log.Information("🔧 Development endpoints enabled: /config/*, /api-docs/*, /scalar/v1, /healthchecks-ui");
    }
    Log.Information("🌟 Proyecto Apuestas application started successfully");
    Log.Information("📊 Health checks available at: /health, /health/ready, /health/live, /healthchecks-ui");
    
    if (app.Environment.IsDevelopment())
    {
        Log.Information("📖 API documentation available at: /scalar/v1");
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Simplified helper class for early Serilog configuration
public class SimpleWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "Proyecto_Apuestas";
    public string WebRootPath { get; set; } = "";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    
    public bool IsDevelopment() => EnvironmentName == "Development";
    public bool IsStaging() => EnvironmentName == "Staging";
    public bool IsProduction() => EnvironmentName == "Production";
}
