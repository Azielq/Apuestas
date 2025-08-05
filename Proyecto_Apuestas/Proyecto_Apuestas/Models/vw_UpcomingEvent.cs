using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Keyless]
public partial class vw_UpcomingEvent
{
    public int EventId { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime Date { get; set; }

    [StringLength(50)]
    public string? ExternalEventId { get; set; }

    [Column(TypeName = "text")]
    public string? Teams { get; set; }

    [StringLength(45)]
    public string Sport { get; set; } = null!;
}
