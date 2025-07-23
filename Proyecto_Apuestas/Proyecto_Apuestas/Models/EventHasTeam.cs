using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[PrimaryKey("EventId", "TeamId")]
[Table("EventHasTeam")]
[Index("TeamId", Name = "FK_EventHasTeam_Team")]
public partial class EventHasTeam
{
    [Key]
    public int EventId { get; set; }

    [Key]
    public int TeamId { get; set; }

    public bool IsHomeTeam { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("EventHasTeams")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("TeamId")]
    [InverseProperty("EventHasTeams")]
    public virtual Team Team { get; set; } = null!;
}
