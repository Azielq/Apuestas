using System.Collections.Concurrent;
using System.Net;

namespace Proyecto_Apuestas.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, RateLimitCounter> _clients;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            RateLimitOptions options = null)
        {
            _next = next;
            _logger = logger;
            _options = options ?? new RateLimitOptions();
            _clients = new ConcurrentDictionary<string, RateLimitCounter>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();

            if (rateLimitAttribute != null || _options.EnableGlobalRateLimit)
            {
                var clientId = GetClientIdentifier(context);
                var rateLimitCounter = _clients.GetOrAdd(clientId, _ => new RateLimitCounter());

                await rateLimitCounter.Lock.WaitAsync();
                try
                {
                    var limit = rateLimitAttribute?.Limit ?? _options.GlobalLimit;
                    var period = TimeSpan.FromSeconds(rateLimitAttribute?.PeriodInSeconds ?? _options.GlobalPeriodInSeconds);

                    rateLimitCounter.RemoveExpiredRequests(period);

                    if (rateLimitCounter.Requests.Count >= limit)
                    {
                        await HandleRateLimitExceeded(context, clientId);
                        return;
                    }

                    rateLimitCounter.Requests.Add(DateTime.UtcNow);
                }
                finally
                {
                    rateLimitCounter.Lock.Release();
                }
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Prioridad: Usuario autenticado > IP Address
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return $"user_{context.User.Identity.Name}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip_{ipAddress}";
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string clientId)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("X-RateLimit-Limit", _options.GlobalLimit.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");
            context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(_options.GlobalPeriodInSeconds).ToUnixTimeSeconds().ToString());

            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
        }

        private class RateLimitCounter
        {
            public List<DateTime> Requests { get; } = new();
            public SemaphoreSlim Lock { get; } = new(1, 1);

            public void RemoveExpiredRequests(TimeSpan period)
            {
                var cutoff = DateTime.UtcNow.Subtract(period);
                Requests.RemoveAll(r => r < cutoff);
            }
        }
    }

    public class RateLimitOptions
    {
        public bool EnableGlobalRateLimit { get; set; } = true;
        public int GlobalLimit { get; set; } = 100;
        public int GlobalPeriodInSeconds { get; set; } = 60;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : Attribute
    {
        public int Limit { get; set; }
        public int PeriodInSeconds { get; set; }

        public RateLimitAttribute(int limit = 10, int periodInSeconds = 60)
        {
            Limit = limit;
            PeriodInSeconds = periodInSeconds;
        }
    }
}
