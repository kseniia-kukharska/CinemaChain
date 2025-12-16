using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CinemaChain.Data.Models;

[Table("Cinema")]
public partial class Cinema
{
    [Key]
    public int CinemaId { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Email { get; set; }

    [StringLength(100)]
    public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
}