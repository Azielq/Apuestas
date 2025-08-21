using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("UserAccount")]
[Index("RoleId", Name = "FK_UserAccount_Role")]
[Index("Email", Name = "UQ_UserAccount_Email", IsUnique = true)]
public partial class UserAccount
{
    [Key]
    public int UserId { get; set; }

    [StringLength(45)]
    public string UserName { get; set; } = null!;

    [StringLength(45)]
    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    [Precision(10, 2)]
    public decimal CreditBalance { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime? LastBet { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [StringLength(200)]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime? LockedUntil { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(45)]
    public string? Country { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(80)]
    public string? FirstName { get; set; }

    [StringLength(45)]
    public string? PrimerApellido { get; set; }

    [StringLength(45)]
    public string? SegundoApellido { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

    [InverseProperty("User")]
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    [InverseProperty("User")]
    public virtual ICollection<ReportLog> ReportLogs { get; set; } = new List<ReportLog>();

    [ForeignKey("RoleId")]
    [InverseProperty("UserAccounts")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<ApiBet> ApiBets { get; set; } = new List<ApiBet>();
}
