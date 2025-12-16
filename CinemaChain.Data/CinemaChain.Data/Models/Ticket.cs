using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

public partial class Ticket
{
    [Key]
    public int TicketId { get; set; }

    public int Row { get; set; }
    public int SeatNumber { get; set; }
    public bool IsSold { get; set; }

    public int SessionId { get; set; }
    [ForeignKey("SessionId")]
    public virtual Session Session { get; set; } = null!;
}