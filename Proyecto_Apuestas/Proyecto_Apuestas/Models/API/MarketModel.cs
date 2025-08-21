namespace Proyecto_Apuestas.Models.API
{
    public class MarketModel
    {
        public string Key { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<OutcomeModel> Outcomes { get; set; }
    }
}
