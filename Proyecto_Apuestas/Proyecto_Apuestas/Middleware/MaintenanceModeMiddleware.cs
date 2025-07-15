using Proyecto_Apuestas.Helpers;

namespace Proyecto_Apuestas.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MaintenanceModeMiddleware> _logger;
        private readonly List<string> _allowedPaths = new()
        {
            "/maintenance",
            "/css/",
            "/js/",
            "/images/",
            "/lib/"
        };

        public MaintenanceModeMiddleware(
            RequestDelegate next,
            ILogger<MaintenanceModeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ConfigurationHelper.SystemSettings.MaintenanceMode)
            {
                // Permitir acceso a admins
                if (context.User.IsInRole("Admin"))
                {
                    context.Response.Headers.Add("X-Maintenance-Mode", "true");
                    await _next(context);
                    return;
                }

                // Permitir ciertas rutas
                if (_allowedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
                {
                    await _next(context);
                    return;
                }

                _logger.LogInformation("Request blocked due to maintenance mode: {Path}", context.Request.Path);

                // Redirigir a página de mantenimiento
                if (!context.Request.Path.Equals("/maintenance"))
                {
                    context.Response.Redirect("/maintenance");
                    return;
                }
            }

            await _next(context);
        }
    }
}
