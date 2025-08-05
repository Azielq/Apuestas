using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("PaymentTransaction")]
[Index("PaymentMethodId", Name = "FK_PaymentTransaction_PaymentMethod")]
[Index("Status", Name = "IX_PaymentTransaction_Status")]
[Index("UserId", "CreatedAt", Name = "IX_PaymentTransaction_UserAccount_CreatedAt", IsDescending = new[] { false, true })]
public partial class PaymentTransaction
{
    [Key]
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public int PaymentMethodId { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string TransactionType { get; set; } = null!;

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("PaymentTransaction")]
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("PaymentTransactions")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PaymentTransactions")]
    public virtual UserAccount User { get; set; } = null!;
}
