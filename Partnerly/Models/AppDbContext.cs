using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Attributes;

namespace Partnerly.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Referrer)
                .WithMany(u => u.Referrals)
                .HasForeignKey(u => u.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);

            #region Create Admin user
            var adminUserId = Guid.NewGuid();
            var adminRoleId = Guid.NewGuid();
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminUserId,
                Email = "marat.iigservices@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("MarDan123!"),
                FirstName = "Marat",
                LastName = "Danielyan",
                Phone = "+37497111312",
                PhotoUrl = null,
                ReferrerId = null,
                RoleId = adminRoleId,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            #endregion
            #region Create Default Roles
            var employeeRoleId = Guid.NewGuid();
            var userRoleId = Guid.NewGuid();
            List<Role> roles = new List<Role>();
            roles.Add(new Role
            {
                Id = adminRoleId,
                Name = "Administrator",
                Type = RoleTypeAttribute.Delete,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            roles.Add(new Role
            {
                Id = employeeRoleId,
                Name = "Employee",
                Type = RoleTypeAttribute.Update,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            roles.Add(new Role
            {
                Id = userRoleId,
                Name = "User",
                Type = RoleTypeAttribute.View,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            modelBuilder.Entity<Role>().HasData(roles);
            #endregion
            #region Create Log
            var logId = Guid.NewGuid();
            modelBuilder.Entity<Log>().HasData(new Log
            {
                Id = logId,
                UserId = adminUserId,
                Action = LogActionsAttribute.UserCreated,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            #endregion
        }
    }
}
