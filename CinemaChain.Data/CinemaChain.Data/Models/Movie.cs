using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

[Table("Movie")]
public partial class Movie
{
    [Key]
    public int MovieId { get; set; } // Ви використовуєте MovieId, а не Id

    [StringLength(255)]
    public string Title { get; set; } = null!;

    public int Duration { get; set; }

    public int AgeRestriction { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public string? Description { get; set; }

    // === ДОДАНО: Списки зв'язків ===
    [InverseProperty("Movie")]
    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

    public virtual ICollection<Session> Session { get; set; } = new List<Session>();
}