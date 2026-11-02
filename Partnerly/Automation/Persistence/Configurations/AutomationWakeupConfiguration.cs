using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationWakeupConfiguration : IEntityTypeConfiguration<AutomationWakeup>
    {
        public void Configure(EntityTypeBuilder<AutomationWakeup> builder)
        {
            builder.ToTable("AutomationWakeups");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Kind).IsRequired().HasMaxLength(30);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.NodeId).HasMaxLength(64);
            builder.Property(x => x.LeaseOwner).HasMaxLength(100);

            builder.HasIndex(x => new { x.Status, x.DueAtUtc })
                .IncludeProperties(x => x.LeaseUntil);

            builder.HasOne<AutomationDefinition>()
                .WithMany()
                .HasForeignKey(x => x.AutomationId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            builder.HasOne<AutomationVersion>()
                .WithMany()
                .HasForeignKey(x => x.VersionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            builder.HasOne<AutomationTrigger>()
                .WithMany()
                .HasForeignKey(x => x.TriggerId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            builder.HasOne<AutomationRun>()
                .WithMany()
                .HasForeignKey(x => x.RunId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        }
    }
}
