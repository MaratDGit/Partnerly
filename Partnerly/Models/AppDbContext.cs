using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
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
        public DbSet<Notification>? Notifications { get; set; }
        public DbSet<UserGroup>? UserGroups { get; set; }
        public DbSet<UserGroupMember>? UserGroupMembers { get; set; }
        public DbSet<SupportTicket>? SupportTickets { get; set; }
        public DbSet<Attachment>? Attachments { get; set; }
        
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<AccessControlAuditLog> AccessControlAuditLogs => Set<AccessControlAuditLog>();

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

            modelBuilder.Entity<UserGroupMember>()
                .HasKey(ugm => new { ugm.UserId, ugm.GroupId });

            modelBuilder.Entity<UserGroupMember>()
                .HasOne(ugm => ugm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(ugm => ugm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserGroupMember>()
                .HasOne(ugm => ugm.User)
                .WithMany() 
                .HasForeignKey(ugm => ugm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attachment>()
                .HasIndex(x => new
                {
                    x.EntityType,
                    x.EntityId,
                    x.IsDeleted
                });

            modelBuilder.Entity<SupportTicket>()
               .HasOne(st => st.Creator)
               .WithMany()
               .HasForeignKey(st => st.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.Assigned)
                .WithMany()
                .HasForeignKey(st => st.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);

            // Permission
            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<Permission>()
                .Property(p => p.Module)
                .IsRequired()
                .HasMaxLength(100);

            // RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new
                {
                    rp.RoleId,
                    rp.PermissionId
                })
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserPermission
            modelBuilder.Entity<UserPermission>()
                .HasIndex(up => new
                {
                    up.UserId,
                    up.PermissionId
                })
                .IsUnique();

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccessControlAuditLog>()
                .HasIndex(x => new
                {
                    x.TargetUserId,
                    x.CreatedDateUtc
                });

            modelBuilder.Entity<AccessControlAuditLog>()
                .HasIndex(x => new
                {
                    x.TargetRoleId,
                    x.CreatedDateUtc
                });

            modelBuilder.Entity<AccessControlAuditLog>()
                .Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();


            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (SkipValidations)
                return await base.SaveChangesAsync(cancellationToken);

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
                        else if (entity is Notification noteEntity)
                        {
                            entity.UpdatedBy = entity.CreatedBy = noteEntity.UserId;
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
                        else if (entity is Notification noteEntity)
                        {
                            entity.UpdatedBy = noteEntity.UserId;
                        }
                    }
                }

                string? result = ValidationHelper.ValidateEntityRequiredFields(entityEntry.Entity, out bool isValid);
                if (!isValid)
                    throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
