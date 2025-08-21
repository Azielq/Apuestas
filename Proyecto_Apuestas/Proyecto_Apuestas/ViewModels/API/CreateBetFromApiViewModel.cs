using System.ComponentModel.DataAnnotations;

namespace Proyecto_Apuestas.ViewModels.API
{
    public class CreateBetFromApiViewModel
    {
        [Required]
        public string ApiEventId { get; set; } = null!;

        [Required]
        public string SportKey { get; set; } = null!;

        [Required]
        public string EventName { get; set; } = null!;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string TeamName { get; set; } = null!;

        [Required]
        [Range(1.01, 50.0, ErrorMessage = "Las cuotas deben estar entre 1.01 y 50.0")]
        public decimal Odds { get; set; }

        [Required]
        [Range(100, 52000000, ErrorMessage = "El monto debe estar entre ₡100 y ₡52,000,000")]
        public decimal Stake { get; set; }

        public decimal UserBalance { get; set; }
        
        public decimal PotentialPayout => Stake * Odds;

        // Betting limit information
        public decimal MaxBetAmount { get; set; }
        public decimal DailyLimit { get; set; }
        public decimal TodayStaked { get; set; }
        public string UserRole { get; set; } = string.Empty;
    }
}
