using CinemaChain.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaChain.Data.Models;

public partial class CinemaDbContext : DbContext
{
    public CinemaDbContext() { }

    public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options) { }

    public virtual DbSet<Cinema> Cinemas { get; set; }
    public virtual DbSet<Genre> Genres { get; set; }
    public virtual DbSet<Hall> Halls { get; set; }
    public virtual DbSet<Movie> Movies { get; set; }
    public virtual DbSet<MovieGenre> MovieGenres { get; set; }
    public virtual DbSet<Session> Session { get; set; }
    public virtual DbSet<Ticket> Tickets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Database=CinemaChain;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // === ВИМОГА: Автоінкремент з великого числа (10000) ===
        modelBuilder.Entity<Cinema>().Property(e => e.CinemaId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);
        modelBuilder.Entity<Genre>().Property(e => e.GenreId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);
        modelBuilder.Entity<Hall>().Property(e => e.HallId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);
        modelBuilder.Entity<Movie>().Property(e => e.MovieId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);
        modelBuilder.Entity<Session>().Property(e => e.SessionId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);
        modelBuilder.Entity<Ticket>().Property(e => e.TicketId).UseIdentityAlwaysColumn().HasIdentityOptions(startValue: 10000);

        // === ВИМОГА: Обмеження (Constraints) та Назви Таблиць ===

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("Movie"); // Явно в однині (хоча EF зазвичай так і робить для Movie)
            entity.HasKey(e => e.MovieId);
            entity.ToTable(t => t.HasCheckConstraint("CK_Movie_Duration", "\"Duration\" > 0"));
            entity.HasIndex(e => e.Title).HasDatabaseName("IX_Movie_Title");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            // ВАЖЛИВО: Примусово називаємо таблицю "Session"
            entity.ToTable("Session");

            entity.HasKey(e => e.SessionId);
            // Constraint прив'язуємо до тієї ж назви таблиці
            entity.ToTable("Session", t => t.HasCheckConstraint("CK_Session_Price", "\"Price\" >= 0"));

            entity.HasOne(d => d.Movie).WithMany(p => p.Session).HasForeignKey(d => d.MovieId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Hall).WithMany(p => p.Session).HasForeignKey(d => d.HallId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            // ВАЖЛИВО: Примусово називаємо таблицю "Ticket"
            entity.ToTable("Ticket");

            entity.HasOne(d => d.Session).WithMany(p => p.Tickets).HasForeignKey(d => d.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Налаштування зв'язків для MovieGenre (без змін)
        modelBuilder.Entity<MovieGenre>(entity =>
        {
            entity.HasKey(e => new { e.MovieId, e.GenreId });
            entity.HasOne(d => d.Movie).WithMany(p => p.MovieGenres).HasForeignKey(d => d.MovieId);
            entity.HasOne(d => d.Genre).WithMany(p => p.MovieGenres).HasForeignKey(d => d.GenreId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}