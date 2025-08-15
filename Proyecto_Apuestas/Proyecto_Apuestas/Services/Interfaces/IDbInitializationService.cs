namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IDbInitializationService
    {
        /// <summary>
        /// Inicializa la base de datos con datos básicos necesarios
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Valida la integridad de los datos básicos
        /// </summary>
        /// <returns>True si la validación es exitosa</returns>
        Task<bool> ValidateDataIntegrityAsync();
    }
}