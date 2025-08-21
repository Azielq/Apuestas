namespace Proyecto_Apuestas.ViewModels.API
{
    public class OddsEventDetailsViewModel
    {
        public Models.API.EventApiModel Event { get; set; }
        public string SportKey { get; set; }
        public decimal UserBalance { get; set; }
        public bool CanBet { get; set; }
    }
}
