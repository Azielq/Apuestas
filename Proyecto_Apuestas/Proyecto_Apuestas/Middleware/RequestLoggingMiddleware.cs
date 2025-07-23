using System.Diagnostics;

namespace Proyecto_Apuestas.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly List<string> _excludedPaths = new()
        {
            "/css/", "/js/", "/images/", "/lib/", "/favicon.ico", "/.well-known/"
        };

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Ignorar archivos estáticos
            if (_excludedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N");

            // Log de entrada
            LogRequest(context, requestId);

            // Capturar el body de la respuesta
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log de salida
                LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copiar la respuesta al stream original
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private void LogRequest(HttpContext context, string requestId)
        {
            var request = context.Request;
            var logData = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                Headers = GetSafeHeaders(request.Headers),
                UserAgent = request.Headers["User-Agent"].ToString(),
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserId = context.User?.Identity?.Name,
                TraceIdentifier = context.TraceIdentifier
            };

            _logger.LogInformation("HTTP Request: {@RequestData}", logData);
        }

        private void LogResponse(HttpContext context, string requestId, long elapsedMs)
        {
            var response = context.Response;
            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            var logData = new
            {
                RequestId = requestId,
                StatusCode = response.StatusCode,
                ElapsedMs = elapsedMs,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength
            };

            _logger.Log(logLevel, "HTTP Response: {@ResponseData}", logData);

            // Log de rendimiento para solicitudes lentas
            if (elapsedMs > 1000)
            {
                _logger.LogWarning("Slow request detected: {Path} took {ElapsedMs}ms",
                    context.Request.Path, elapsedMs);
            }
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key" };

            foreach (var header in headers)
            {
                if (sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                {
                    safeHeaders[header.Key] = "***REDACTED***";
                }
                else
                {
                    safeHeaders[header.Key] = header.Value.ToString();
                }
            }

            return safeHeaders;
        }
    }
}