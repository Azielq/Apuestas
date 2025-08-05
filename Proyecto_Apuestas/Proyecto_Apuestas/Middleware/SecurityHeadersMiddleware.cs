namespace Proyecto_Apuestas.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersOptions _options;

        public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options = null)
        {
            _next = next;
            _options = options ?? new SecurityHeadersOptions();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Agregar headers de seguridad antes de procesar la respuesta
            AddSecurityHeaders(context);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Content Security Policy
            if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // X-Content-Type-Options
            headers["X-Content-Type-Options"] = "nosniff";

            // X-Frame-Options
            headers["X-Frame-Options"] = _options.XFrameOptions;

            // X-XSS-Protection
            headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer-Policy
            headers["Referrer-Policy"] = _options.ReferrerPolicy;

            // Strict-Transport-Security (HSTS)
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = _options.StrictTransportSecurity;
            }

            // Permissions-Policy
            if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
            {
                headers["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // Remover headers que exponen información
            headers.Remove("X-Powered-By");
            headers.Remove("Server");
        }
    }

    public class SecurityHeadersOptions
    {
        public string ContentSecurityPolicy { get; set; } =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://code.jquery.com; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' https://api.proyectoapuestas.com wss://proyectoapuestas.com; " +
            "frame-ancestors 'none';";

        public string XFrameOptions { get; set; } = "DENY";

        public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

        public string StrictTransportSecurity { get; set; } = "max-age=31536000; includeSubDomains; preload";

        public string PermissionsPolicy { get; set; } =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    }
}