using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("LoginAttempt")]
[Index("UserId", "AttemptTime", Name = "IX_LoginAttempt_UserAccount_AttemptTime", IsDescending = new[] { false, true })]
public partial class LoginAttempt
{
    [Key]
    public int AttemptId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime AttemptTime { get; set; }

    public bool IsSuccessful { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("LoginAttempts")]
    public virtual UserAccount User { get; set; } = null!;
}
