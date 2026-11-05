using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence.Configurations
{
    public class AutomationFavoriteConfiguration : IEntityTypeConfiguration<AutomationFavorite>
    {
        public void Configure(EntityTypeBuilder<AutomationFavorite> builder)
        {
            builder.ToTable("AutomationFavorites");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemKey).IsRequired().HasMaxLength(200);
            builder.HasIndex(x => new { x.UserId, x.ItemKey }).IsUnique();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
