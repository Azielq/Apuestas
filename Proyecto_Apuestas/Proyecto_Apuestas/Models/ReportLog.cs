using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("ReportLog")]
[Index("UserId", Name = "FK_ReportLog_UserAccount")]
public partial class ReportLog
{
    [Key]
    public int ReportId { get; set; }

    public int UserId { get; set; }

    [StringLength(50)]
    public string ReportType { get; set; } = null!;

    [Column(TypeName = "datetime(3)")]
    public DateTime GeneratedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ReportLogs")]
    public virtual UserAccount User { get; set; } = null!;
}
