using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

[PrimaryKey("MovieId", "GenreId")]
[Table("MovieGenre")]
public partial class MovieGenre
{
    [Key]
    public int MovieId { get; set; }

    [Key]
    public int GenreId { get; set; }

    // === ДОДАНО: Навігаційні властивості ===
    [ForeignKey("MovieId")]
    public virtual Movie Movie { get; set; } = null!;

    [ForeignKey("GenreId")]
    public virtual Genre Genre { get; set; } = null!;
}