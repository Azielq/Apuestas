using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Team")]
[Index("SportId", Name = "FK_Team_Sport")]
public partial class Team
{
    [Key]
    public int TeamId { get; set; }

    public int SportId { get; set; }

    [StringLength(45)]
    public string TeamName { get; set; } = null!;

    [Precision(5, 2)]
    public decimal TeamWinPercent { get; set; }

    [Precision(5, 2)]
    public decimal TeamDrawPercent { get; set; }

    public DateOnly? LastWin { get; set; }

    [Required]
    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Team")]
    public virtual ICollection<EventHasTeam> EventHasTeams { get; set; } = new List<EventHasTeam>();

    [InverseProperty("Team")]
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    [InverseProperty("Team")]
    public virtual ICollection<OddsHistory> OddsHistories { get; set; } = new List<OddsHistory>();

    [ForeignKey("SportId")]
    [InverseProperty("Teams")]
    public virtual Sport Sport { get; set; } = null!;
}
