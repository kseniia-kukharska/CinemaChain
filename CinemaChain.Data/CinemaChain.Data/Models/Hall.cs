using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

[Table("Hall")]
public partial class Hall
{
    [Key]
    public int HallId { get; set; }

    public int CinemaId { get; set; }

    public int SeatsCount { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [ForeignKey("CinemaId")]
    public virtual Cinema Cinema { get; set; } = null!;

    public virtual ICollection<Session> Session { get; set; } = new List<Session>();
}