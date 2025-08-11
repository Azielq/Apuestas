namespace Proyecto_Apuestas.ViewModels.API
{
    public class CreateBetFromApiViewModel
    {
        public string ApiEventId { get; set; }
        public string SportKey { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string TeamName { get; set; }
        public decimal Odds { get; set; }
        public decimal Stake { get; set; }
        public decimal UserBalance { get; set; }
        public decimal PotentialPayout => Stake * Odds;
    }
}
