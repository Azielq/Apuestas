using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Helpers;
using Proyecto_Apuestas.ViewModels;
using System.Net;
using System.Text.Json;

namespace Proyecto_Apuestas.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ApiResponseViewModel<object>
            {
                Success = false,
                Message = "Ha ocurrido un error en el servidor",
                Errors = new List<string>()
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "No autorizado";
                    break;

                case KeyNotFoundException:
                case FileNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = "Recurso no encontrado";
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = "Solicitud inválida";
                    if (_environment.IsDevelopment())
                    {
                        errorResponse.Errors.Add(exception.Message);
                    }
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Message = "La solicitud ha excedido el tiempo límite";
                    break;

                case DbUpdateException:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Error al procesar la operación en la base de datos";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Ha ocurrido un error inesperado";
                    break;
            }

            // En desarrollo, incluir detalles del error
            if (_environment.IsDevelopment() || ConfigurationHelper.SystemSettings.EnableDetailedErrors)
            {
                errorResponse.Data = new
                {
                    type = exception.GetType().Name,
                    message = exception.Message,
                    stackTrace = exception.StackTrace
                };
            }

            // Log del error con contexto adicional
            LogError(context, exception, response.StatusCode);

            // Si es una petición AJAX, devolver JSON
            if (IsAjaxRequest(context.Request))
            {
                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await response.WriteAsync(jsonResponse);
            }
            else
            {
                // Para peticiones normales, redirigir a página de error
                context.Response.Redirect($"/Home/Error?statusCode={response.StatusCode}");
            }
        }

        private void LogError(HttpContext context, Exception exception, int statusCode)
        {
            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                RequestId = context.TraceIdentifier,
                UserId = context.User?.Identity?.Name,
                Path = context.Request.Path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString(),
                StatusCode = statusCode,
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message,
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            };

            _logger.LogError(exception, "Error details: {@ErrorData}", logData);
        }

        private bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.ContentType?.Contains("application/json") == true ||
                   request.Path.StartsWithSegments("/api");
        }
    }
}
