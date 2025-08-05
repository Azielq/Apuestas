using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("PaymentMethod")]
[Index("UserId", Name = "FK_PaymentMethod_UserAccount")]
public partial class PaymentMethod
{
    [Key]
    public int PaymentMethodId { get; set; }

    public int UserId { get; set; }

    [StringLength(100)]
    public string ProviderName { get; set; } = null!;

    [StringLength(100)]
    public string AccountReference { get; set; } = null!;

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [Required]
    public bool? IsActive { get; set; }

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    [ForeignKey("UserId")]
    [InverseProperty("PaymentMethods")]
    public virtual UserAccount User { get; set; } = null!;
}
