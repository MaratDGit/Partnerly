using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<User>? Users { get; set; }
        public DbSet<Role>? Roles { get; set; }
        public DbSet<Transaction>? Transactions { get; set; }
        public DbSet<Payment>? Payments { get; set; }
        public DbSet<Log>? Logs { get; set; }
        public DbSet<EmailConfirmationToken>? EmailConfirmationTokens { get; set; }
        public DbSet<SystemSettings>? SystemSettings { get; set; }
        public DbSet<EmailTemplate>? EmailTemplates { get; set; }
        public DbSet<EmailAttachment>? EmailAttachments { get; set; }
        

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
                Email = Constants.SuperUserEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("MarDan123!"),
                FirstName = "Marat",
                LastName = "Danielyan",
                Phone = "+37497111312",
                PhotoUrl = null,
                MyReferralCode = Constants.SuperReferralCode,
                ReferrerId = null,
                RoleId = adminRoleId,
                IsBlocked = false,
                IsDeleted = false,
                EmailConfirmed = true,
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
                Name = RoleTypeAttribute.Admin,
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
                Name = RoleTypeAttribute.Employee,
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
                Name = RoleTypeAttribute.User,
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
                Action = LogActionsAttribute.UserCreated,
                Type = LogTypeAttribute.Information,
                LogMessage = "Created the Admin user from OnModelCreating",
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            #endregion
            #region Create System Settings
            modelBuilder.Entity<SystemSettings>().HasData(new SystemSettings
            {
                Id = 1,
                EmailConfirmationTokenExpiredAtHours = 24,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            #endregion
            #region Create Email Templates
            string body = @"
                            <h2>Здравствуйте, {{UserName}}!</h2>
                            <p>
                                Подтвердите ваш email, перейдя по ссылке:
                                <a href=""{{ConfirmationLink}}"">Подтвердить</a>
                            </p>";

            modelBuilder.Entity<EmailTemplate>().HasData(new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = EmailTemplateNameAttribute.EmailConfirmation,
                Subject = "Подтверждение регистрации",
                BodyHtml = body,
                IsDeleted = false,
                CreatedBy = adminUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = adminUserId,
                UpdatedDate = DateTime.UtcNow,
            });
            #endregion
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (IAuditableEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.UpdatedBy = _currentUserService.UserId;
                    entity.CreatedBy = _currentUserService.UserId;
                    entity.CreatedDate = DateTime.UtcNow;
                    entity.UpdatedDate = DateTime.UtcNow;

                    if (_currentUserService.UserId == null)
                    {
                        if (entity is User userEntity)
                        {
                            entity.UpdatedBy = entity.CreatedBy = userEntity.Id;
                        }
                        else if (entity is EmailConfirmationToken tokenEntity)
                        {
                            entity.UpdatedBy = entity.CreatedBy = tokenEntity.UserId;
                        }
                        else if (entity is Log logEntity)
                        {
                            User? superUser = Users?.FirstOrDefault(_ => _.Email == Constants.SuperUserEmail);
                            entity.UpdatedBy = entity.CreatedBy = superUser?.Id ?? new Guid();
                        }
                    }
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    entity.UpdatedBy = _currentUserService.UserId;
                    entity.UpdatedDate = DateTime.UtcNow;

                    if (_currentUserService.UserId == null)
                    {
                        if (entity is User userEntity)
                        {
                            entity.UpdatedBy = userEntity.Id;
                        }
                        else if (entity is EmailConfirmationToken tokenEntity)
                        {
                            entity.UpdatedBy = tokenEntity.UserId;
                        }
                    }
                }
                else if (entityEntry.State == EntityState.Deleted)
                {
                    
                }

                string? result = ValidationHelper.ValidateEntityRequiredFields(entityEntry.Entity, out bool isValid);

                if (!isValid)
                    throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));


            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
