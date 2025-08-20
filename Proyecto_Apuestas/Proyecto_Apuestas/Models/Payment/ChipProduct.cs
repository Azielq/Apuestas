using System.ComponentModel.DataAnnotations;

namespace Proyecto_Apuestas.Models.Payment
{
    public class ChipProduct
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Precio opcionalmente para mostrar en UI; el cobro real lo hace Stripe por PriceId
        [Required]
        public int PriceInCents { get; set; } // $15.00 = 1500

        // IMPORTANTE: Debe ser un Price ID de Stripe (empieza con price_)
        [Required]
        public string StripePriceId { get; set; } = string.Empty;

        [Required]
        public int Chips { get; set; } // Cantidad de chips que otorga

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}

