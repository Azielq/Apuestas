using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Apuestas.Models
{
    public class Bet
    {
        public int BetId { get; set; }
        public int UserId { get; set; }
        public int MatchId { get; set; }
        public decimal Payout { get; set; }
        public decimal Odds { get; set; }
        public int ProviderId { get; set; } // e.g., "Pending", "Won", "Lost"
    }
}
