using Proyecto_Apuestas.Helpers;
using System.Globalization;

namespace Proyecto_Apuestas.Middleware
{
    public class LocalizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LocalizationMiddleware> _logger;

        public LocalizationMiddleware(
            RequestDelegate next,
            ILogger<LocalizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var supportedCultures = new[] { "es-CR", "es", "en-US", "en" };
            var defaultCulture = ConfigurationHelper.SystemSettings.DefaultCulture;

            var requestCulture = GetRequestCulture(context, supportedCultures, defaultCulture);

            var cultureInfo = new CultureInfo(requestCulture);
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;

            // Agregar header para informar la cultura actual
            context.Response.Headers.Add("Content-Language", requestCulture);

            await _next(context);
        }

        private string GetRequestCulture(HttpContext context, string[] supportedCultures, string defaultCulture)
        {
            // 1. Verificar cookie de preferencia
            var cookieCulture = context.Request.Cookies["culture"];
            if (!string.IsNullOrEmpty(cookieCulture) && supportedCultures.Contains(cookieCulture))
            {
                return cookieCulture;
            }

            // 2. Verificar query string
            var queryCulture = context.Request.Query["culture"].ToString();
            if (!string.IsNullOrEmpty(queryCulture) && supportedCultures.Contains(queryCulture))
            {
                SetCultureCookie(context, queryCulture);
                return queryCulture;
            }

            // 3. Verificar Accept-Language header
            var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var languages = acceptLanguage.Split(',')
                    .Select(lang => lang.Trim().Split(';')[0])
                    .ToList();

                foreach (var lang in languages)
                {
                    var matchingCulture = supportedCultures.FirstOrDefault(c =>
                        c.StartsWith(lang, StringComparison.OrdinalIgnoreCase));

                    if (matchingCulture != null)
                    {
                        return matchingCulture;
                    }
                }
            }

            // 4. Usar cultura por defecto
            return defaultCulture;
        }

        private void SetCultureCookie(HttpContext context, string culture)
        {
            context.Response.Cookies.Append("culture", culture, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Strict,
                HttpOnly = true
            });
        }
    }
}
