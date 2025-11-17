using Demo.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Database;

public sealed class FingerprintContext : DbContext
{
    public FingerprintContext()
    {
    }

    public FingerprintContext(DbContextOptions<FingerprintContext> options)
        : base(options)
    {
        Database.SetCommandTimeout(TimeSpan.FromHours(2));
    }

    public DbSet<Image> Images { get; set; }


    public DbSet<Minutia> Minutiae { get; set; }

    public DbSet<TestRun> TestRuns { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost;Database=Fingerprint;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=0");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.TestRun).WithMany(p => p.Images)
                .HasForeignKey(d => d.TestRunId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Minutia>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Minutiae");

            entity.Property(e => e.IsTermination)
                .HasColumnType("BIT")
                .HasConversion(i => i != 0, b => b ? 1 : 0);

            entity.HasOne(d => d.Image).WithMany(p => p.Minutiae)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TestRun>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StartDate).HasPrecision(3);
        });
    }
}
