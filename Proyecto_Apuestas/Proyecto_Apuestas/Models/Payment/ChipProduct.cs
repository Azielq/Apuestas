using System.ComponentModel.DataAnnotations;

namespace Proyecto_Apuestas.Models.Payment
{
    public class ChipProduct
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Precio en centavos/centimos (minor units) para Stripe.
        /// Ej: ₡1.000 => 100.000
        /// </summary>
        [Required]
        public int PriceInCents { get; set; }

        /// <summary>
        /// Cantidad de "chips" que otorga. En este esquema = colones (1:1)
        /// </summary>
        [Required]
        public int Chips { get; set; }

        /// <summary>
        /// Opcional: usa un Price pre-creado en Stripe. Si está vacío, se usa inline price.
        /// </summary>
        [StringLength(120)]
        public string? StripePriceId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
