using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationDefinitionConfiguration : IEntityTypeConfiguration<AutomationDefinition>
    {
        public void Configure(EntityTypeBuilder<AutomationDefinition> builder)
        {
            builder.ToTable("Automations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Note).HasMaxLength(2000);

            builder.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
            builder.HasIndex(x => new { x.IsActive, x.IsDeleted });

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AutomationVersion>()
                .WithMany()
                .HasForeignKey(x => x.PublishedVersionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            builder.HasOne<AutomationVersion>()
                .WithMany()
                .HasForeignKey(x => x.DraftVersionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        }
    }
}
