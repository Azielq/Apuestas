using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;

namespace Proyecto_Apuestas.Services.Implementations
{
    public interface IHealthCheckService
    {
        Task<bool> CheckDatabaseConnectionAsync();
        Task<bool> CheckExternalApiConnectionAsync();
        Task<Dictionary<string, bool>> CheckAllServicesAsync();
    }

    public class HealthCheckService : IHealthCheckService
    {
        private readonly apuestasDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IConfiguration _configuration;

        public HealthCheckService(
            apuestasDbContext context,
            HttpClient httpClient,
            ILogger<HealthCheckService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                // Intenta una consulta simple para verificar la conexión
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return false;
            }
        }

        public async Task<bool> CheckExternalApiConnectionAsync()
        {
            try
            {
                var apiUrl = _configuration["OddsApi:BaseUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                    return false;

                var response = await _httpClient.GetAsync($"{apiUrl}/sports", HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API health check failed");
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> CheckAllServicesAsync()
        {
            var results = new Dictionary<string, bool>();

            // Verifica base de datos
            results["Database"] = await CheckDatabaseConnectionAsync();

            // Verifica API externa
            results["ExternalAPI"] = await CheckExternalApiConnectionAsync();

            // Verifica archivos de configuración
            results["Configuration"] = CheckConfiguration();

            return results;
        }

        private bool CheckConfiguration()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var apiKey = _configuration["OddsApi:ApiKey"];
                
                return !string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(apiKey);
            }
            catch
            {
                return false;
            }
        }
    }
}