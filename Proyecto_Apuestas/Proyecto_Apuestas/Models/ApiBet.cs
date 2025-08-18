using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("ApiBet")]
[Index("ApiEventId", Name = "IX_ApiBet_ApiEventId")]
[Index("PaymentTransactionId", Name = "FK_ApiBet_PaymentTxn")]
[Index("BetStatus", Name = "IX_ApiBet_BetStatus")]
[Index("Date", Name = "IX_ApiBet_Date")]
public partial class ApiBet
{
    [Key]
    public int ApiBetId { get; set; }

    [Required]
    [StringLength(100)]
    public string ApiEventId { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string SportKey { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string EventName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TeamName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime EventDate { get; set; }

    [Precision(10, 2)]
    public decimal Odds { get; set; }

    [Precision(10, 2)]
    public decimal Payout { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime Date { get; set; }

    [Precision(10, 2)]
    public decimal Stake { get; set; }

    [StringLength(1)]
    public string BetStatus { get; set; } = null!; // P=Pending, W=Won, L=Lost, C=Cancelled

    public int? PaymentTransactionId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    // Información adicional del evento API
    [StringLength(100)]
    public string? HomeTeam { get; set; }

    [StringLength(100)]
    public string? AwayTeam { get; set; }

    [StringLength(50)]
    public string? Region { get; set; }

    [StringLength(50)]
    public string? Market { get; set; }

    [StringLength(100)]
    public string? Bookmaker { get; set; }

    // Resultado del evento (JSON serializado)
    [Column(TypeName = "TEXT")]
    public string? EventResult { get; set; }

    [ForeignKey("PaymentTransactionId")]
    [InverseProperty("ApiBets")]
    public virtual PaymentTransaction? PaymentTransaction { get; set; }

    [ForeignKey("ApiBetId")]
    [InverseProperty("ApiBets")]
    public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}