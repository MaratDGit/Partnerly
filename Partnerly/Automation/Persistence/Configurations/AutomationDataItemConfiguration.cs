using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationDataItemConfiguration : IEntityTypeConfiguration<AutomationDataItem>
    {
        public void Configure(EntityTypeBuilder<AutomationDataItem> builder)
        {
            builder.ToTable("AutomationDataItems");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Value).IsRequired();
            builder.HasIndex(x => new { x.AutomationId, x.Key }).IsUnique();

            builder.HasOne<AutomationDefinition>()
                .WithMany()
                .HasForeignKey(x => x.AutomationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
