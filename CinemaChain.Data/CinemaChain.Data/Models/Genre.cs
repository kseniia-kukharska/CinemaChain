using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaChain.Data.Models;

[Table("Genre")]
public partial class Genre
{
    [Key]
    public int GenreId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
