using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Microsoft.Extensions.Logging;

namespace Proyecto_Apuestas.Configuration
{
    /// <summary>
    /// Configuration class for Serilog logging setup
    /// </summary>
    public static class LoggingConfiguration
    {
        /// <summary>
        /// Configures Serilog with multiple sinks and enrichers
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="environment">Web host environment</param>
        /// <returns>Configured LoggerConfiguration</returns>
        public static LoggerConfiguration ConfigureSerilog(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "Proyecto_Apuestas")
                .Enrich.WithProperty("Version", "1.0.0");

            // Configure minimum level based on environment
            var minimumLevel = environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;
            loggerConfig.MinimumLevel.Is(minimumLevel);

            // Override levels for specific namespaces
            loggerConfig
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning);

            // Console sink configuration
            if (environment.IsDevelopment())
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug
                );
            }
            else
            {
                // Production console output in JSON format for better parsing
                loggerConfig.WriteTo.Console(
                    new CompactJsonFormatter(),
                    restrictedToMinimumLevel: LogEventLevel.Information
                );
            }

            // File sink configuration - Text logs
            var logPath = configuration["Logging:FilePath"] ?? "logs/app-.log";
            loggerConfig.WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties}{NewLine}{Exception}",
                fileSizeLimitBytes: 50_000_000, // 50MB
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Information
            );

            // Structured JSON file for analysis and monitoring tools
            var jsonLogPath = configuration["Logging:JsonFilePath"] ?? "logs/app-.json";
            loggerConfig.WriteTo.File(
                new CompactJsonFormatter(),
                path: jsonLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 100_000_000, // 100MB
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Information
            );

            // Error-only log file for critical issues
            var errorLogPath = configuration["Logging:ErrorFilePath"] ?? "logs/errors-.log";
            loggerConfig.WriteTo.File(
                path: errorLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90, // Keep error logs longer
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000, // 10MB
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error
            );

            // Performance logging for slow operations
            if (environment.IsDevelopment())
            {
                var performanceLogPath = configuration["Logging:PerformanceFilePath"] ?? "logs/performance-.log";
                loggerConfig.WriteTo.File(
                    path: performanceLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Message:lj}{NewLine}",
                    fileSizeLimitBytes: 25_000_000, // 25MB
                    rollOnFileSizeLimit: true,
                    shared: true
                );
            }

            return loggerConfig;
        }

        /// <summary>
        /// Configures request logging middleware settings for Razor Pages
        /// </summary>
        /// <param name="options">Request logging options</param>
        public static void ConfigureRequestLogging(Serilog.AspNetCore.RequestLoggingOptions options)
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            
            // Configure log levels based on response
            options.GetLevel = (httpContext, elapsed, ex) => 
            {
                // Skip health check endpoints from detailed logging
                if (httpContext.Request.Path.StartsWithSegments("/health") && 
                    httpContext.Response.StatusCode == 200)
                {
                    return LogEventLevel.Verbose; // Will be filtered out by minimum level
                }

                // Skip static files
                if (httpContext.Request.Path.StartsWithSegments("/css") ||
                    httpContext.Request.Path.StartsWithSegments("/js") ||
                    httpContext.Request.Path.StartsWithSegments("/images") ||
                    httpContext.Request.Path.StartsWithSegments("/favicon.ico"))
                {
                    return LogEventLevel.Verbose; // Will be filtered out by minimum level
                }

                return ex != null
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode > 499
                        ? LogEventLevel.Error
                        : httpContext.Response.StatusCode > 399
                            ? LogEventLevel.Warning
                            : elapsed > 5000 // Log as warning if request takes more than 5 seconds
                                ? LogEventLevel.Warning
                                : LogEventLevel.Information;
            };

            // Enhanced diagnostic context for Razor Pages with safe session access
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                try
                {
                    // Basic request information
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
                    diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);

                    // Razor Pages specific information
                    if (httpContext.Request.RouteValues.ContainsKey("page"))
                    {
                        diagnosticContext.Set("RazorPage", httpContext.Request.RouteValues["page"]);
                    }

                    if (httpContext.Request.RouteValues.ContainsKey("action"))
                    {
                        diagnosticContext.Set("Action", httpContext.Request.RouteValues["action"]);
                    }

                    if (httpContext.Request.RouteValues.ContainsKey("controller"))
                    {
                        diagnosticContext.Set("Controller", httpContext.Request.RouteValues["controller"]);
                    }

                    // User authentication information
                    if (httpContext.User?.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set("UserId", httpContext.User.FindFirst("UserId")?.Value);
                        diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                        diagnosticContext.Set("UserRoles", string.Join(",", httpContext.User.Claims
                            .Where(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                            .Select(c => c.Value)));
                    }

                    // Request/Response size information
                    if (httpContext.Request.ContentLength.HasValue)
                    {
                        diagnosticContext.Set("RequestLength", httpContext.Request.ContentLength.Value);
                    }

                    if (httpContext.Response.ContentLength.HasValue)
                    {
                        diagnosticContext.Set("ResponseLength", httpContext.Response.ContentLength.Value);
                    }

                    // Safe session information access (for betting applications)
                    try
                    {
                        // Check if session is configured and available
                        var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
                        if (sessionFeature?.Session != null)
                        {
                            diagnosticContext.Set("SessionId", sessionFeature.Session.Id);
                            
                            // Add betting-specific session data if available
                            var betSlipCount = sessionFeature.Session.GetString("BetSlipCount");
                            if (!string.IsNullOrEmpty(betSlipCount))
                            {
                                diagnosticContext.Set("BetSlipCount", betSlipCount);
                            }

                            // Add other betting session data safely
                            var currentBetAmount = sessionFeature.Session.GetString("CurrentBetAmount");
                            if (!string.IsNullOrEmpty(currentBetAmount))
                            {
                                diagnosticContext.Set("CurrentBetAmount", currentBetAmount);
                            }

                            var userPreferences = sessionFeature.Session.GetString("UserPreferences");
                            if (!string.IsNullOrEmpty(userPreferences))
                            {
                                diagnosticContext.Set("HasUserPreferences", true);
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Session not configured - this is fine, just skip session logging
                        diagnosticContext.Set("SessionAvailable", false);
                    }
                    catch (Exception sessionEx)
                    {
                        // Log other session errors but don't fail the entire enrichment
                        diagnosticContext.Set("SessionError", sessionEx.GetType().Name);
                    }

                    // API specific information
                    if (httpContext.Request.Path.StartsWithSegments("/api"))
                    {
                        diagnosticContext.Set("IsApiRequest", true);
                        diagnosticContext.Set("ApiVersion", httpContext.Request.Headers["X-API-Version"].FirstOrDefault());
                    }

                    // Performance markers
                    diagnosticContext.Set("ResponseCached", httpContext.Response.Headers.ContainsKey("Cache-Control"));
                    diagnosticContext.Set("IsHealthCheck", httpContext.Request.Path.StartsWithSegments("/health"));

                    // Additional Razor Pages context
                    if (httpContext.Request.Path.StartsWithSegments("/Pages") || 
                        httpContext.Request.RouteValues.ContainsKey("page"))
                    {
                        diagnosticContext.Set("IsRazorPage", true);
                        
                        // Extract area if present
                        if (httpContext.Request.RouteValues.ContainsKey("area"))
                        {
                            diagnosticContext.Set("Area", httpContext.Request.RouteValues["area"]);
                        }
                    }

                    // Betting application specific context
                    if (httpContext.Request.Path.StartsWithSegments("/Betting") ||
                        httpContext.Request.Path.StartsWithSegments("/Odds") ||
                        httpContext.Request.Path.StartsWithSegments("/Event"))
                    {
                        diagnosticContext.Set("IsBettingRequest", true);
                    }
                }
                catch (Exception ex)
                {
                    // If enrichment fails, log the error but don't crash the request
                    diagnosticContext.Set("EnrichmentError", ex.GetType().Name);
                    diagnosticContext.Set("EnrichmentMessage", ex.Message);
                }
            };
        }

        /// <summary>
        /// Creates performance logger for timing operations
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <returns>Disposable that logs performance when disposed</returns>
        public static IDisposable LogPerformance(string operationName)
        {
            return new PerformanceLogger(operationName);
        }

        /// <summary>
        /// Safely logs session information without throwing exceptions
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="key">Session key to retrieve</param>
        /// <returns>Session value or null if not available</returns>
        public static string? GetSessionValueSafely(HttpContext httpContext, string key)
        {
            try
            {
                var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
                return sessionFeature?.Session?.GetString(key);
            }
            catch (InvalidOperationException)
            {
                // Session not configured
                return null;
            }
            catch
            {
                // Any other error
                return null;
            }
        }

        /// <summary>
        /// Extension method to safely set session values
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="key">Session key</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SetSessionValueSafely(HttpContext httpContext, string key, string value)
        {
            try
            {
                var sessionFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>();
                if (sessionFeature?.Session != null)
                {
                    sessionFeature.Session.SetString(key, value);
                    return true;
                }
                return false;
            }
            catch (InvalidOperationException)
            {
                // Session not configured
                return false;
            }
            catch
            {
                // Any other error
                return false;
            }
        }

        /// <summary>
        /// Helper class for performance logging
        /// </summary>
        private class PerformanceLogger : IDisposable
        {
            private readonly string _operationName;
            private readonly DateTime _startTime;
            private readonly Serilog.ILogger _logger;

            public PerformanceLogger(string operationName)
            {
                _operationName = operationName;
                _startTime = DateTime.UtcNow;
                _logger = Log.ForContext("SourceContext", "Performance");
            }

            public void Dispose()
            {
                var elapsed = DateTime.UtcNow - _startTime;
                _logger.Information("Operation {OperationName} completed in {ElapsedMs} ms", 
                    _operationName, elapsed.TotalMilliseconds);
            }
        }
    }
}