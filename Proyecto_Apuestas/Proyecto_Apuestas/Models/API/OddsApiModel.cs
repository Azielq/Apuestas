using Newtonsoft.Json;

namespace Proyecto_Apuestas.Models.API
{
    public class OddsApiModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("sport_key")]
        public string SportKey { get; set; }
        
        [JsonProperty("sport_title")]
        public string SportTitle { get; set; }
        
        [JsonProperty("commence_time")]
        public DateTime CommenceTime { get; set; }
        
        [JsonProperty("home_team")]
        public string HomeTeam { get; set; }
        
        [JsonProperty("away_team")]
        public string AwayTeam { get; set; }
        
        [JsonProperty("bookmakers")]
        public List<BookmakerModel> Bookmakers { get; set; }
    }
}
