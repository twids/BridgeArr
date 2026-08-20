using BridgeArr.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for BridgeArr.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();
    public DbSet<SyncRoute> SyncRoutes => Set<SyncRoute>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<PluginConfiguration> PluginConfigurations => Set<PluginConfiguration>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Integration>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PluginType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ConfigurationJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.PluginType);
        });

        builder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.SourceIntegration)
                .WithMany()
                .HasForeignKey(x => x.SourceIntegrationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetIntegration)
                .WithMany()
                .HasForeignKey(x => x.TargetIntegrationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.Status);
        });
        builder.Entity<SyncRoute>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.SourceIntegration)
                .WithMany()
                .HasForeignKey(x => x.SourceIntegrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TargetIntegration)
                .WithMany()
                .HasForeignKey(x => x.TargetIntegrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SourceIntegrationId, x.TargetIntegrationId }).IsUnique();
            entity.HasIndex(x => x.Enabled);
        });

        builder.Entity<SyncHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.SyncJob)
                .WithMany()
                .HasForeignKey(x => x.SyncJobId);
        });

        builder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Processed);
        });

        builder.Entity<PluginConfiguration>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Integration)
                .WithMany()
                .HasForeignKey(x => x.IntegrationId);
            entity.Property(x => x.ConfigurationJson).HasColumnType("jsonb");
        });

        builder.Entity<ApplicationSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });
    }
}
