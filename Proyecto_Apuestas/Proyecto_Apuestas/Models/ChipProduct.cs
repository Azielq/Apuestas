using System.ComponentModel.DataAnnotations;

namespace Proyecto_Apuestas.Models
{
    public class ChipProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int PriceInCents { get; set; } //Precio en Centavos $15 = 15000


        [Required]
        public int Chips { get; set; } // Cantidad de chips que otorga

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
