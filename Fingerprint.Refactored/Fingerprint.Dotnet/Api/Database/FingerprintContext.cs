using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Database;

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

    public DbSet<Cluster> Clusters { get; set; }

    public DbSet<ClusterComparison> ClusterComparisons { get; set; }

    public DbSet<ClusterMinutiae> ClusterMinutiae { get; set; }

    public DbSet<Image> Images { get; set; }

    public DbSet<Metric> Metrics { get; set; }

    public DbSet<Minutia> Minutiae { get; set; }

    public DbSet<MinutiaeMetric> MinutiaeMetrics { get; set; }

    public DbSet<TestRun> TestRuns { get; set; }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //     => optionsBuilder.UseSqlServer("Data Source=host.docker.internal,1433;Initial Catalog=Fingerprint;User Id=sa;Password=abcDEF123#;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cluster>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Image).WithMany(p => p.Clusters)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClusterComparison>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.FirstMinutia).WithMany(p => p.ClusterComparisonFirstMinutia)
                .HasForeignKey(d => d.FirstMinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SecondMinutia).WithMany(p => p.ClusterComparisonSecondMinutia)
                .HasForeignKey(d => d.SecondMinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.LeadingFirstMinutia)
                .WithMany(cm => cm.ClusterComparisonLeadingFirstMinutia)
                .HasForeignKey(d => d.LeadingFirstMinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            
            entity.HasOne(d => d.LeadingSecondMinutia)
                .WithMany(cm => cm.ClusterComparisonLeadingSecondMinutia)
                .HasForeignKey(d => d.LeadingSecondMinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClusterMinutiae>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("ClusterMinutiae");

            entity.HasOne(d => d.Cluster).WithMany(p => p.ClusterMinutiaes)
                .HasForeignKey(d => d.ClusterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Minutia).WithMany(p => p.ClusterMinutiae)
                .HasForeignKey(d => d.MinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.TestRun).WithMany(p => p.Images)
                .HasForeignKey(d => d.TestRunId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(900);
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

        modelBuilder.Entity<MinutiaeMetric>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Cluster).WithMany(p => p.MinutiaeMetrics)
                .HasForeignKey(d => d.ClusterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Metric).WithMany(p => p.MinutiaeMetrics)
                .HasForeignKey(d => d.MetricId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Minutia).WithMany(p => p.MinutiaeMetrics)
                .HasForeignKey(d => d.MinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OtherMinutia).WithMany(p => p.MinutiaeMetricOthers)
                .HasForeignKey(d => d.OtherMinutiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TestRun>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StartDate).HasPrecision(3);
        });
    }
}
