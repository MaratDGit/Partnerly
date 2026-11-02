using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationRunConfiguration : IEntityTypeConfiguration<AutomationRun>
    {
        public void Configure(EntityTypeBuilder<AutomationRun> builder)
        {
            builder.ToTable("AutomationRuns");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TriggerType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.TriggerReason).HasMaxLength(200);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
            builder.Property(x => x.ContextJson).IsRequired();
            builder.Property(x => x.ChosenPortsJson).IsRequired();
            builder.Property(x => x.CompletedNodeIdsJson).IsRequired();

            builder.HasIndex(x => new { x.OwnerUserId, x.CreatedDate });
            builder.HasIndex(x => new { x.AutomationId, x.Status });
            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AutomationDefinition>()
                .WithMany()
                .HasForeignKey(x => x.AutomationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AutomationVersion>()
                .WithMany()
                .HasForeignKey(x => x.VersionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
