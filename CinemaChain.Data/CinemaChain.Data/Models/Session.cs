using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

public partial class Session
{
    [Key]
    public int SessionId { get; set; }

    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }

    public int MovieId { get; set; }
    [ForeignKey("MovieId")]
    public virtual Movie Movie { get; set; } = null!;

    public int HallId { get; set; }
    [ForeignKey("HallId")]
    public virtual Hall Hall { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}