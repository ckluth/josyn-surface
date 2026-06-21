using Microsoft.EntityFrameworkCore;

namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a read-only EF Core context over the DEV josyn-db-local database.
// Mirrors the SessionStore convention (internal sealed; DbContext never public). Confined to a
// single DEV installation — the connection string is supplied by the caller and never switched at
// runtime (ADR-010: environment = installation). Removed when the real agent lands.
internal sealed class SurfaceReadDbContext(string connectionString) : DbContext
{
    public DbSet<SessionRow>          Sessions           => Set<SessionRow>();
    public DbSet<ErrorRow>            Errors             => Set<ErrorRow>();
    public DbSet<JobRegistrationRow>  JobRegistrations   => Set<JobRegistrationRow>();
    public DbSet<ArgumentRecordRow>   ArgumentRecords    => Set<ArgumentRecordRow>();
    public DbSet<JobScheduleRow>      JobSchedules       => Set<JobScheduleRow>();
    public DbSet<JobScheduleEntryRow> JobScheduleEntries => Set<JobScheduleEntryRow>();

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

        modelBuilder.Entity<JobRegistrationRow>(e =>
        {
            e.ToTable("JobRegistry", "josyn");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Name).IsRequired().HasMaxLength(256);
            e.Property(x => x.TechnicalUserName).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<ArgumentRecordRow>(e =>
        {
            e.ToTable("ArgumentRecords", "josyn");
            e.HasKey(x => new { x.JobName, x.Name });
            e.Property(x => x.JobName).IsRequired().HasMaxLength(256);
            e.Property(x => x.Name).IsRequired().HasMaxLength(256);
            e.Property(x => x.Content).IsRequired();

            e.HasOne(x => x.Registration)
             .WithMany(x => x.ArgumentRecords)
             .HasForeignKey(x => x.JobName)
             .HasPrincipalKey(x => x.Name);
        });

        modelBuilder.Entity<JobScheduleRow>(e =>
        {
            e.ToTable("JobSchedules", "josyn");
            e.HasKey(x => x.JobName);
            e.Property(x => x.JobName).IsRequired().HasMaxLength(256);
            e.Property(x => x.Suspended).IsRequired();
            e.Property(x => x.SuspendedUntil).HasColumnType("date");
        });

        modelBuilder.Entity<JobScheduleEntryRow>(e =>
        {
            e.ToTable("JobScheduleEntries", "josyn");
            e.HasKey(x => new { x.JobName, x.ArgumentRecordName });
            e.Property(x => x.JobName).IsRequired().HasMaxLength(256);
            e.Property(x => x.ArgumentRecordName).IsRequired().HasMaxLength(256);
            e.Property(x => x.ScheduleDefinition).IsRequired();
            e.Property(x => x.ToleranceMinutes);

            e.HasOne(x => x.Schedule)
             .WithMany(x => x.Entries)
             .HasForeignKey(x => x.JobName);
        });
    }
}
