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
        [Range(100, 50000, ErrorMessage = "El monto debe estar entre ₡100 y ₡50,000")]
        public decimal Stake { get; set; }

        public decimal UserBalance { get; set; }
        
        public decimal PotentialPayout => Stake * Odds;
    }
}
