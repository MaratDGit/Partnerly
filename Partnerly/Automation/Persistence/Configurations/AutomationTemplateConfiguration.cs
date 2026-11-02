using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationTemplateConfiguration : IEntityTypeConfiguration<AutomationTemplate>
    {
        public void Configure(EntityTypeBuilder<AutomationTemplate> builder)
        {
            builder.ToTable("AutomationTemplates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.GraphJson).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(2000);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
