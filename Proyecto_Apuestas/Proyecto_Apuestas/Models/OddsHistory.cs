using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("OddsHistory")]
[Index("TeamId", Name = "FK_OddsHistory_Team")]
[Index("EventId", "TeamId", "RetrievedAt", Name = "IX_OddsHistory_Event_Team_RetrievedAt", IsDescending = new[] { false, false, true })]
public partial class OddsHistory
{
    [Key]
    public int OddsId { get; set; }

    public int EventId { get; set; }

    public int TeamId { get; set; }

    [Precision(5, 2)]
    public decimal Odds { get; set; }

    [StringLength(100)]
    public string Source { get; set; } = null!;

    [Column(TypeName = "datetime(3)")]
    public DateTime RetrievedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("OddsHistories")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("TeamId")]
    [InverseProperty("OddsHistories")]
    public virtual Team Team { get; set; } = null!;
}
