using Newtonsoft.Json;

namespace Proyecto_Apuestas.Models.API
{
    public class EventApiModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("sport_key")]
        public string SportKey { get; set; }
        
        [JsonProperty("sport_title")]
        public string SportTitle { get; set; }
        
        [JsonProperty("commence_time")]
        public DateTime CommenceTime { get; set; }
        
        [JsonProperty("completed")]
        public bool Completed { get; set; }
        
        [JsonProperty("home_team")]
        public string HomeTeam { get; set; }
        
        [JsonProperty("away_team")]
        public string AwayTeam { get; set; }
        
        [JsonProperty("bookmakers")]
        public List<BookmakerModel> Bookmakers { get; set; }
        
        [JsonProperty("scores")]
        public ScoreModel? Scores { get; set; }
    }
}
