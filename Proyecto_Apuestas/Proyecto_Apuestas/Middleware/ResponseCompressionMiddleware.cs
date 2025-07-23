using System.IO.Compression;

namespace Proyecto_Apuestas.Middleware
{
    public class ResponseCompressionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CompressionOptions _options;

        public ResponseCompressionMiddleware(
            RequestDelegate next,
            CompressionOptions options = null)
        {
            _next = next;
            _options = options ?? new CompressionOptions();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();

            if (!ShouldCompress(context) || string.IsNullOrEmpty(acceptEncoding))
            {
                await _next(context);
                return;
            }

            var originalBody = context.Response.Body;

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (context.Response.ContentLength > _options.MinimumSize)
            {
                if (acceptEncoding.Contains("gzip"))
                {
                    context.Response.Headers.Add("Content-Encoding", "gzip");
                    context.Response.Body = originalBody;

                    using var compressionStream = new GZipStream(originalBody, CompressionLevel.Optimal);
                    await memoryStream.CopyToAsync(compressionStream);
                }
                else if (acceptEncoding.Contains("br"))
                {
                    context.Response.Headers.Add("Content-Encoding", "br");
                    context.Response.Body = originalBody;

                    using var compressionStream = new BrotliStream(originalBody, CompressionLevel.Optimal);
                    await memoryStream.CopyToAsync(compressionStream);
                }
                else
                {
                    context.Response.Body = originalBody;
                    await memoryStream.CopyToAsync(originalBody);
                }
            }
            else
            {
                context.Response.Body = originalBody;
                await memoryStream.CopyToAsync(originalBody);
            }
        }

        private bool ShouldCompress(HttpContext context)
        {
            if (context.Response.Headers.ContainsKey("Content-Encoding"))
                return false;

            var contentType = context.Response.ContentType?.ToLower() ?? "";

            return _options.CompressibleMimeTypes.Any(mime => contentType.Contains(mime));
        }
    }

    public class CompressionOptions
    {
        public int MinimumSize { get; set; } = 1024; // 1KB

        public HashSet<string> CompressibleMimeTypes { get; set; } = new()
        {
            "text/plain",
            "text/css",
            "text/html",
            "text/javascript",
            "application/javascript",
            "application/json",
            "application/xml",
            "text/xml"
        };
    }
}
