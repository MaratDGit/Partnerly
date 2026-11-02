using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationTriggerConfiguration : IEntityTypeConfiguration<AutomationTrigger>
    {
        public void Configure(EntityTypeBuilder<AutomationTrigger> builder)
        {
            builder.ToTable("AutomationTriggers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ConfigJson).IsRequired();
            builder.Property(x => x.WebhookPublicId).HasMaxLength(40);
            builder.Property(x => x.WebhookSecretHash).HasMaxLength(200);

            builder.HasIndex(x => new { x.Type, x.IsEnabled });
            builder.HasIndex(x => x.WebhookPublicId)
                .IsUnique()
                .HasFilter("[WebhookPublicId] IS NOT NULL");

            builder.HasOne<AutomationVersion>()
                .WithMany()
                .HasForeignKey(x => x.VersionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
