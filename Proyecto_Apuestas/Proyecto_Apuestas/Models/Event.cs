using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Event")]
public partial class Event
{
    [Key]
    public int EventId { get; set; }

    [StringLength(50)]
    public string? ExternalEventId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime Date { get; set; }

    [StringLength(45)]
    public string Outcome { get; set; } = null!;

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Event")]
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();

    [InverseProperty("Event")]
    public virtual ICollection<EventHasTeam> EventHasTeams { get; set; } = new List<EventHasTeam>();

    [InverseProperty("Event")]
    public virtual ICollection<OddsHistory> OddsHistories { get; set; } = new List<OddsHistory>();
}
