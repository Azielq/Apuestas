using Scalar.AspNetCore;

namespace Proyecto_Apuestas.Configuration
{
    /// <summary>
    /// Configuration for Scalar API documentation
    /// </summary>
    public static class ScalarConfiguration
    {
        /// <summary>
        /// Configures Scalar API documentation for the application
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddScalarDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            // Add OpenAPI/Swagger services
            services.AddOpenApi();
            
            return services;
        }

        /// <summary>
        /// Maps Scalar endpoints for API documentation
        /// </summary>
        /// <param name="app">Web application</param>
        /// <param name="environment">Web host environment</param>
        /// <returns>Web application</returns>
        public static WebApplication UseScalarDocumentation(this WebApplication app, IWebHostEnvironment environment)
        {
            // Only enable in development and staging environments for security
            if (environment.IsDevelopment() || environment.IsStaging())
            {
                // Map OpenAPI
                app.MapOpenApi();

                // Configure Scalar UI
                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Proyecto Apuestas API")
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        .WithSearchHotKey("ctrl+k")
                        .WithPreferredScheme("https")
                        .WithModels(true)
                        .WithDownloadButton(true)
                        .WithSidebar(true);
                });

                // Health checks documentation endpoint
                app.MapGet("/api-docs/health", () => new
                {
                    title = "Health Checks API",
                    description = "Comprehensive health monitoring endpoints for the Proyecto Apuestas application",
                    version = "1.0.0",
                    endpoints = new object[]
                    {
                        new
                        {
                            path = "/health",
                            method = "GET",
                            description = "Overall application health status with all checks",
                            tags = new[] { "Health", "Monitoring" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "Application is healthy" },
                                ["503"] = new { description = "Application is unhealthy" }
                            }
                        },
                        new
                        {
                            path = "/health/ready",
                            method = "GET",
                            description = "Readiness probe for container orchestration",
                            tags = new[] { "Health", "Kubernetes" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "Application is ready to serve traffic" },
                                ["503"] = new { description = "Application is not ready" }
                            }
                        },
                        new
                        {
                            path = "/health/live",
                            method = "GET",
                            description = "Liveness probe for container orchestration",
                            tags = new[] { "Health", "Kubernetes" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "Application is alive" },
                                ["503"] = new { description = "Application needs restart" }
                            }
                        },
                        new
                        {
                            path = "/health/database",
                            method = "GET",
                            description = "Database connectivity health check",
                            tags = new[] { "Health", "Database" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "Database is accessible" },
                                ["503"] = new { description = "Database connection failed" }
                            }
                        },
                        new
                        {
                            path = "/health/external",
                            method = "GET",
                            description = "External services health check",
                            tags = new[] { "Health", "External" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "External services are accessible" },
                                ["503"] = new { description = "External services connection failed" }
                            }
                        },
                        new
                        {
                            path = "/health/system",
                            method = "GET",
                            description = "System resources health check",
                            tags = new[] { "Health", "System" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "System resources are normal" },
                                ["503"] = new { description = "System resources are critical" }
                            }
                        },
                        new
                        {
                            path = "/healthchecks-ui",
                            method = "GET",
                            description = "Health Checks UI dashboard",
                            tags = new[] { "Health", "UI" },
                            responses = new Dictionary<string, object>
                            {
                                ["200"] = new { description = "Health checks dashboard" }
                            }
                        }
                    }
                })
                .WithName("HealthChecksDocumentation")
                .WithSummary("Health Checks API Documentation")
                .WithDescription("Detailed documentation for all health check endpoints")
                .WithTags("Documentation", "Health");

                // Logging configuration documentation endpoint
                app.MapGet("/api-docs/logging", () => new
                {
                    title = "Logging Configuration",
                    description = "Serilog configuration and logging endpoints for the Proyecto Apuestas application",
                    version = "1.0.0",
                    sinks = new object[]
                    {
                        new
                        {
                            name = "Console",
                            description = "Console output with structured logging",
                            environment = "All environments",
                            format = "Development: Plain text, Production: JSON"
                        },
                        new
                        {
                            name = "File",
                            description = "Rolling file logs with retention policy",
                            path = "logs/app-.log",
                            retention = "30 days",
                            maxSize = "50MB per file"
                        },
                        new
                        {
                            name = "JSON File",
                            description = "Structured JSON logs for analysis",
                            path = "logs/app-.json",
                            retention = "7 days",
                            maxSize = "100MB per file"
                        }
                    },
                    enrichers = new[]
                    {
                        "Environment Name",
                        "Machine Name",
                        "Process ID",
                        "Process Name",
                        "Thread ID",
                        "Application Name",
                        "Version",
                        "Request Context"
                    },
                    logLevels = new
                    {
                        Default = "Development: Debug, Production: Information",
                        Microsoft = "Warning",
                        EntityFrameworkCore = "Warning",
                        System = "Warning"
                    }
                })
                .WithName("LoggingDocumentation")
                .WithSummary("Logging Configuration Documentation")
                .WithDescription("Detailed documentation for Serilog configuration and logging setup")
                .WithTags("Documentation", "Logging");
            }

            return app;
        }
    }
}