using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Competition")]
[Index("SportId", Name = "FK_Competition_Sport")]
public partial class Competition
{
    [Key]
    public int CompetitionId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public int SportId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Required]
    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Competition")]
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    [ForeignKey("SportId")]
    [InverseProperty("Competitions")]
    public virtual Sport Sport { get; set; } = null!;
}
