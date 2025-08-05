using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UserActivityMiddleware> _logger;
        private readonly List<string> _trackedPaths = new()
        {
            "/betting", "/event", "/payment", "/sport", "/team"
        };

        public UserActivityMiddleware(
            RequestDelegate next,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<UserActivityMiddleware> logger)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Registrar actividad después de procesar la solicitud
            if (context.User.Identity?.IsAuthenticated == true &&
                _trackedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
            {
                _ = Task.Run(async () => await TrackUserActivity(context));
            }
        }

        private async Task TrackUserActivity(HttpContext context)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<apuestasDbContext>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var userId = userService.GetCurrentUserId();
                if (userId > 0)
                {
                    var user = await dbContext.UserAccounts.FindAsync(userId);
                    if (user != null)
                    {
                        user.UpdatedAt = DateTime.Now;
                        await dbContext.SaveChangesAsync();
                    }

                    // Log de actividad para análisis
                    _logger.LogInformation("User activity: UserId={UserId}, Path={Path}, Time={Time}",
                        userId, context.Request.Path, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user activity");
            }
        }
    }
}