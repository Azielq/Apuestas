using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Apuestas.Models;

[Table("ApiBetUserAccount")]
public class ApiBetUserAccount
{
    public int ApiBetId { get; set; }
    public int UserId { get; set; }

    public ApiBet ApiBet { get; set; } = null!;
    public UserAccount User { get; set; } = null!;
}
