using Microsoft.AspNetCore.Identity;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.Services;
using FluentValidation;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;

namespace Proyecto_Apuestas.Configuration
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure settings
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));
            services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));

            // AutoMapper for version 15.0.1
            services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));

            // HttpContextAccessor for accessing the current HTTP context
            services.AddHttpContextAccessor();

            // Password hasher for user authentication
            services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();

            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<Program>();

            // SendGrid configuration
            var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>();
            if (!string.IsNullOrEmpty(emailSettings?.SendGrid?.ApiKey))
            {
                services.AddSendGrid(options =>
                {
                    options.ApiKey = emailSettings.SendGrid.ApiKey;
                });
            }
            else
            {
                // Fallback registration for development
                services.AddSingleton<ISendGridClient>(provider => 
                    new SendGridClient("your-api-key-here"));
            }

            // System services
            services.AddScoped<IStartupValidationService, StartupValidationService>();
            services.AddScoped<IConfigurationDiagnosticsService, ConfigurationDiagnosticsService>();

            // Application services
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBettingService, BettingService>();
            services.AddScoped<IApiBettingService, ApiBettingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IOddsService, OddsService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IEmailService, EmailService>();

            // Configure session for bet slip functionality
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "Bet506.Session";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Configure authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Cookies";
                options.DefaultSignInScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Cookie.Name = "Bet506.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ReturnUrlParameter = "returnUrl";
            });

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                
                options.AddPolicy("ModeratorOrAbove", policy =>
                    policy.RequireRole("Admin", "Moderator"));
                
                options.AddPolicy("RegularUser", policy =>
                    policy.RequireRole("Admin", "Moderator", "Regular"));

                options.AddPolicy("ApiAccess", policy =>
                    policy.RequireAuthenticatedUser());
            });

            // Add CORS for API endpoints
            services.AddCors(options =>
            {
                options.AddPolicy("ApiPolicy", builder =>
                {
                    builder.WithOrigins("https://localhost:7000", "https://bet506.com")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            return services;
        }
    }
}