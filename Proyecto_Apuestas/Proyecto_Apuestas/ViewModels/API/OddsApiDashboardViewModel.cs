namespace Proyecto_Apuestas.ViewModels.API
{
    public class OddsApiDashboardViewModel
    {
        public List<Models.API.SportApiModel> ActiveSports { get; set; }
        public Models.API.ApiUsageModel ApiUsage { get; set; }
        public bool IsApiAvailable { get; set; }
    }
}
