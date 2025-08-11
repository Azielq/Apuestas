namespace Proyecto_Apuestas.ViewModels.API
{
    public class SportEventsViewModel
    {
        public Models.API.SportApiModel Sport { get; set; }
        public List<Models.API.OddsApiModel> Events { get; set; }
        public string Region { get; set; }
        public string Market { get; set; }
        public string SportKey { get; set; }
    }
}
