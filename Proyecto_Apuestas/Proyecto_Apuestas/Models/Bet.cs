using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Bet")]
[Index("EventId", Name = "FK_Bet_Event")]
[Index("PaymentTransactionId", Name = "FK_Bet_PaymentTxn")]
[Index("BetStatus", Name = "IX_Bet_BetStatus")]
[Index("Date", Name = "IX_Bet_Date")]
public partial class Bet
{
    [Key]
    public int BetId { get; set; }

    public int EventId { get; set; }

    [Precision(10, 2)]
    public decimal Odds { get; set; }

    [Precision(10, 2)]
    public decimal Payout { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime Date { get; set; }

    [Precision(10, 2)]
    public decimal Stake { get; set; }

    [StringLength(1)]
    public string BetStatus { get; set; } = null!;

    public int? PaymentTransactionId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("Bets")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("PaymentTransactionId")]
    [InverseProperty("Bets")]
    public virtual PaymentTransaction? PaymentTransaction { get; set; }

    [ForeignKey("BetId")]
    [InverseProperty("Bets")]
    public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}
