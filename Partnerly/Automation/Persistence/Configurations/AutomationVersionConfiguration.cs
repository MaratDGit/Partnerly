using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationVersionConfiguration : IEntityTypeConfiguration<AutomationVersion>
    {
        public void Configure(EntityTypeBuilder<AutomationVersion> builder)
        {
            builder.ToTable("AutomationVersions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.GraphJson).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(2000);
            builder.HasIndex(x => new { x.AutomationId, x.Number }).IsUnique();

            builder.HasOne<AutomationDefinition>()
                .WithMany()
                .HasForeignKey(x => x.AutomationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
