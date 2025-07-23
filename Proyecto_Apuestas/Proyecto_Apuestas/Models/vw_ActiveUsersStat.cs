using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Models;

[Keyless]
public partial class vw_ActiveUsersStat
{
    public int UserId { get; set; }

    [StringLength(45)]
    public string UserName { get; set; } = null!;

    [StringLength(45)]
    public string Email { get; set; } = null!;

    [StringLength(45)]
    public string RoleName { get; set; } = null!;

    [Precision(10, 2)]
    public decimal CreditBalance { get; set; }

    public long TotalBets { get; set; }

    [Precision(23, 0)]
    public decimal? WonBets { get; set; }

    [Precision(23, 0)]
    public decimal? LostBets { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime? LastBet { get; set; }

    [Column(TypeName = "datetime(3)")]
    public DateTime CreatedAt { get; set; }
}
