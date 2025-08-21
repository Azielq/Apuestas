namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IDbInitializationService
    {
        /// <summary>
        /// Inicializa la base de datos con datos b�sicos necesarios
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Valida la integridad de los datos b�sicos
        /// </summary>
        /// <returns>True si la validaci�n es exitosa</returns>
        Task<bool> ValidateDataIntegrityAsync();
    }
}