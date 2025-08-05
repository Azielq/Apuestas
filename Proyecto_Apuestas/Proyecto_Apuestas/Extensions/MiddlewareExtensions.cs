using Proyecto_Apuestas.Middleware;

namespace Proyecto_Apuestas.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder, SecurityHeadersOptions options = null)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>(options);
        }

        public static IApplicationBuilder UseUserActivity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserActivityMiddleware>();
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitOptions options = null)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>(options);
        }

        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceModeMiddleware>();
        }

        public static IApplicationBuilder UseLocalization(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LocalizationMiddleware>();
        }



        public static IApplicationBuilder UseResponseCompression(this IApplicationBuilder builder, CompressionOptions options = null)
        {
            return builder.UseMiddleware<ResponseCompressionMiddleware>(options);
        }
    }
}