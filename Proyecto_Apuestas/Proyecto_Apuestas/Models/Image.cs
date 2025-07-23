using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Table("Image")]
[Index("CompetitionId", Name = "FK_Image_Competition")]
[Index("TeamId", Name = "FK_Image_Team")]
public partial class Image
{
    [Key]
    public int ImageId { get; set; }

    [StringLength(2083)]
    public string Url { get; set; } = null!;

    public int? TeamId { get; set; }

    public int? CompetitionId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompetitionId")]
    [InverseProperty("Images")]
    public virtual Competition? Competition { get; set; }

    [ForeignKey("TeamId")]
    [InverseProperty("Images")]
    public virtual Team? Team { get; set; }
}
