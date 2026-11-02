using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationConnectionConfiguration : IEntityTypeConfiguration<AutomationConnection>
    {
        public void Configure(EntityTypeBuilder<AutomationConnection> builder)
        {
            builder.ToTable("AutomationConnections");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Kind).IsRequired().HasMaxLength(50);
            builder.Property(x => x.EncryptedPayload).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(2000);
            builder.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
