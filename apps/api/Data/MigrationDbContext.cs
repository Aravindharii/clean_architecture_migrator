using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureMigration.Api.Data;

public class MigrationDbContext : DbContext
{
    public MigrationDbContext(DbContextOptions<MigrationDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<MigrationJobEntity> MigrationJobs => Set<MigrationJobEntity>();
    public DbSet<AnalysisRunEntity> AnalysisRuns => Set<AnalysisRunEntity>();
    public DbSet<MigrationPlanEntity> MigrationPlans => Set<MigrationPlanEntity>();
    public DbSet<ApprovalEntity> Approvals => Set<ApprovalEntity>();
    public DbSet<TransformationRunEntity> TransformationRuns => Set<TransformationRunEntity>();
    public DbSet<ValidationResultEntity> ValidationResults => Set<ValidationResultEntity>();
    public DbSet<ArtifactEntity> Artifacts => Set<ArtifactEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProjectEntity>()
            .HasOne(p => p.User)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MigrationJobEntity>()
            .HasOne(j => j.Project)
            .WithMany(p => p.Jobs)
            .HasForeignKey(j => j.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AnalysisRunEntity>()
            .HasOne(a => a.Job)
            .WithOne(j => j.AnalysisRun)
            .HasForeignKey<AnalysisRunEntity>(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MigrationPlanEntity>()
            .HasOne(p => p.Job)
            .WithMany(j => j.Plans)
            .HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApprovalEntity>()
            .HasOne(a => a.Job)
            .WithMany(j => j.Approvals)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransformationRunEntity>()
            .HasOne(t => t.Job)
            .WithOne(j => j.TransformationRun)
            .HasForeignKey<TransformationRunEntity>(t => t.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ValidationResultEntity>()
            .HasOne(v => v.Job)
            .WithOne(j => j.ValidationResult)
            .HasForeignKey<ValidationResultEntity>(v => v.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArtifactEntity>()
            .HasOne(a => a.Job)
            .WithMany(j => j.Artifacts)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditEventEntity>()
            .HasOne(e => e.Job)
            .WithMany(j => j.AuditEvents)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<MigrationJobEntity>()
            .HasIndex(j => j.ProjectId);
        modelBuilder.Entity<MigrationJobEntity>()
            .HasIndex(j => j.SourceHash);
        modelBuilder.Entity<MigrationPlanEntity>()
            .HasIndex(p => new { p.JobId, p.PlanVersion });
        modelBuilder.Entity<ApprovalEntity>()
            .HasIndex(a => new { a.PlanId, a.SourceHash, a.PlanVersion });
    }
}
