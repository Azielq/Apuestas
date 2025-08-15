using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class DatabaseInitializationService : IDbInitializationService
    {
        private readonly apuestasDbContext _context;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            apuestasDbContext context,
            ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Verificar conexión a la base de datos
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("No se puede conectar a la base de datos");
                    return;
                }

                // Aplica migraciones pendientes (opcional)
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Aplicando {Count} migraciones pendientes", pendingMigrations.Count());
                    await _context.Database.MigrateAsync();
                }

                // Inicializa roles
                await InitializeRolesAsync();

                // Inicializa datos básicos
                await InitializeBasicDataAsync();

                _logger.LogInformation("Inicialización de base de datos completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la inicialización de la base de datos");
                throw;
            }
        }

        private async Task InitializeRolesAsync()
        {
            var requiredRoles = new[]
            {
                new { Name = "Admin", Description = "Administrador del sistema con acceso completo" },
                new { Name = "Regular", Description = "Usuario regular con permisos básicos" },
                new { Name = "Moderator", Description = "Moderador con permisos intermedios" },
                new { Name = "VIP", Description = "Usuario VIP con permisos especiales" }
            };

            foreach (var roleInfo in requiredRoles)
            {
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == roleInfo.Name);

                if (existingRole == null)
                {
                    var newRole = new Role
                    {
                        RoleName = roleInfo.Name
                    };

                    _context.Roles.Add(newRole);
                    _logger.LogInformation("Creando rol: {RoleName}", roleInfo.Name);
                }
            }

            var changes = await _context.SaveChangesAsync();
            if (changes > 0)
            {
                _logger.LogInformation("Se crearon {Count} roles nuevos", changes);
            }
        }

        private async Task InitializeBasicDataAsync()
        {
            // Verifica e inicializa deportes básicos
            await InitializeSportsAsync();
        }

        private async Task InitializeSportsAsync()
        {
            var requiredSports = new[]
            {
                new { Name = "Fútbol", IsActive = true },
                new { Name = "Baloncesto", IsActive = true },
                new { Name = "Tenis", IsActive = true },
                new { Name = "Béisbol", IsActive = true },
                new { Name = "Fútbol Americano", IsActive = true },
                new { Name = "Hockey", IsActive = false },
                new { Name = "Voleibol", IsActive = true }
            };

            foreach (var sportInfo in requiredSports)
            {
                var existingSport = await _context.Sports
                    .FirstOrDefaultAsync(s => s.Name == sportInfo.Name);

                if (existingSport == null)
                {
                    var newSport = new Sport
                    {
                        Name = sportInfo.Name,
                        IsActive = sportInfo.IsActive,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Sports.Add(newSport);
                    _logger.LogInformation("Creando deporte: {SportName}", sportInfo.Name);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateDataIntegrityAsync()
        {
            try
            {
                // Verifica que existan los roles básicos
                var requiredRoles = new[] { "Admin", "Regular" };
                foreach (var roleName in requiredRoles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                    if (role == null)
                    {
                        _logger.LogError("Rol requerido no encontrado: {RoleName}", roleName);
                        return false;
                    }
                }

                // Verifica que existan deportes activos
                var activeSports = await _context.Sports.CountAsync(s => s.IsActive == true);
                if (activeSports == 0)
                {
                    _logger.LogWarning("No hay deportes activos configurados");
                }

                _logger.LogInformation("Validación de integridad completada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la validación de integridad de datos");
                return false;
            }
        }
    }
}