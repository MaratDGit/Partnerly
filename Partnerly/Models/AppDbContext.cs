using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class AppDbContext : DbContext
    {
        public bool SkipValidations { get; set; } = false;
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
        public DbSet<Notification>? Notifications { get; set; }

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
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!SkipValidations)
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
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
