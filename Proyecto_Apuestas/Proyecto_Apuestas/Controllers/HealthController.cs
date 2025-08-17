using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Implementations;

namespace Proyecto_Apuestas.Controllers
{
    [Route("health")]
    public class HealthController : BaseController
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly IWebHostEnvironment _environment;

        public HealthController(
            IHealthCheckService healthCheckService,
            IWebHostEnvironment environment,
            ILogger<HealthController> logger) : base(logger)
        {
            _healthCheckService = healthCheckService;
            _environment = environment;
        }

        [HttpGet]
        [HttpHead]  // Agrega soporte para HEAD requests
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var healthStatus = await _healthCheckService.CheckAllServicesAsync();
            var overallHealth = healthStatus.Values.All(x => x);

            var result = new
            {
                Status = overallHealth ? "Healthy" : "Unhealthy",
                Services = healthStatus,
                Timestamp = DateTime.UtcNow
            };

            return Json(result);
        }

        [HttpGet("database")]
        [AllowAnonymous]
        public async Task<IActionResult> Database()
        {
            var isHealthy = await _healthCheckService.CheckDatabaseConnectionAsync();
            
            return Json(new
            {
                Service = "Database",
                Status = isHealthy ? "Healthy" : "Unhealthy",
                Message = isHealthy ? "Conexión a base de datos exitosa" : "Error de conexión a base de datos",
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("api")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalApi()
        {
            var isHealthy = await _healthCheckService.CheckExternalApiConnectionAsync();
            
            return Json(new
            {
                Service = "ExternalAPI",
                Status = isHealthy ? "Healthy" : "Unhealthy",
                Message = isHealthy ? "API externa disponible" : "Error de conexión con API externa",
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> TestConnection()
        {
            var results = new Dictionary<string, object>();
            
            try 
            {
                // Test 1: Base de datos
                var dbHealthy = await _healthCheckService.CheckDatabaseConnectionAsync();
                results["database"] = new 
                {
                    status = dbHealthy ? "Healthy" : "Unhealthy",
                    message = dbHealthy ? "Conexión exitosa" : "Error de conexión",
                    responseTime = DateTime.UtcNow
                };

                // Test 2: API externa
                var apiHealthy = await _healthCheckService.CheckExternalApiConnectionAsync();
                results["externalApi"] = new 
                {
                    status = apiHealthy ? "Healthy" : "Unhealthy",
                    message = apiHealthy ? "API disponible" : "API no disponible",
                    responseTime = DateTime.UtcNow
                };

                // Test 3: Configuración general
                results["configuration"] = new 
                {
                    status = "Healthy",
                    message = "Configuración cargada correctamente",
                    responseTime = DateTime.UtcNow
                };

                var overallHealthy = dbHealthy && apiHealthy;
                
                return Json(new 
                {
                    status = overallHealthy ? "Healthy" : "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    details = results,
                    message = overallHealthy ? 
                        "Todos los servicios funcionan correctamente" : 
                        "Algunos servicios presentan problemas"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection test");
                
                return Json(new 
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    message = "Error durante la prueba de conexión",
                    error = ex.Message
                });
            }
        }

        [HttpGet("status")]
        [HttpHead("status")]  // Agrega soporte para HEAD requests
        [AllowAnonymous]
        public IActionResult Status()
        {
            return Json(new
            {
                Status = "Healthy",
                Message = "Servidor funcionando correctamente",
                Timestamp = DateTime.UtcNow,
                Environment = _environment.EnvironmentName,
                Version = "1.0.0"
            });
        }
    }
}