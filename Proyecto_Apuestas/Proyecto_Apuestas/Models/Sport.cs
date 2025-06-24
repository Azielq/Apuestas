using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Sport")]
public partial class Sport
{
    [Key]
    public int SportId { get; set; }

    [StringLength(45)]
    public string Name { get; set; } = null!;

    [Required]
    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Sport")]
    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    [InverseProperty("Sport")]
    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
