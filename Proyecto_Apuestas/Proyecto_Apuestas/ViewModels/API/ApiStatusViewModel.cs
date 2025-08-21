namespace Proyecto_Apuestas.ViewModels.API
{
    public class ApiStatusViewModel
    {
        public bool IsAvailable { get; set; }
        public int RequestsUsed { get; set; }
        public int RequestsRemaining { get; set; }
        public decimal UsagePercentage { get; set; }    
    }
}
