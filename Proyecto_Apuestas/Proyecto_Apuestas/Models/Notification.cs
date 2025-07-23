using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Notification")]
[Index("UserId", "IsRead", Name = "IX_Notification_UserAccount_IsRead")]
public partial class Notification
{
    [Key]
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "text")]
    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual UserAccount User { get; set; } = null!;
}
