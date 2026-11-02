using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationRunStepConfiguration : IEntityTypeConfiguration<AutomationRunStep>
    {
        public void Configure(EntityTypeBuilder<AutomationRunStep> builder)
        {
            builder.ToTable("AutomationRunSteps");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.NodeId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.ActionType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => new { x.RunId, x.NodeId, x.Attempt }).IsUnique();

            builder.HasOne<AutomationRun>()
                .WithMany()
                .HasForeignKey(x => x.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
