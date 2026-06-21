using Microsoft.EntityFrameworkCore;

namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a read-only EF Core context over the DEV josyn-db-local database.
// Mirrors the SessionStore convention (internal sealed; DbContext never public). Confined to a
// single DEV installation — the connection string is supplied by the caller and never switched at
// runtime (ADR-010: environment = installation). Removed when the real agent lands.
internal sealed class SurfaceReadDbContext(string connectionString) : DbContext
{
    public DbSet<SessionRow> Sessions => Set<SessionRow>();
    public DbSet<ErrorRow>   Errors   => Set<ErrorRow>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionRow>(e =>
        {
            e.ToTable("SessionStore", "josyn");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExecutionStatus).HasMaxLength(32).IsUnicode(false);
        });

        modelBuilder.Entity<ErrorRow>(e =>
        {
            e.ToTable("ErrorStore", "josyn");
            e.HasKey(x => x.Id);
        });
    }
}
