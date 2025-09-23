using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            var adminUserId = Guid.NewGuid();
            var adminRoleId = Guid.NewGuid();
            if (context.Users != null && context.Users.Count() == 0 && !context.Users.Any(u => u.Email == Constants.SuperUserEmail))
            {
                context.Users.Add(new User
                {
                    Id = adminUserId,
                    Email = Constants.SuperUserEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Constants.SuperUserPassword),
                    FirstName = Constants.SuperUserFirstName,
                    LastName = Constants.SuperUserLastName,
                    Phone = Constants.SuperUserPhone,
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

                context.Roles.Add(new Role
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
                context.Roles.Add(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = RoleTypeAttribute.Employee,
                    Type = RoleTypeAttribute.Update,
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });
                context.Roles.Add(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = RoleTypeAttribute.User,
                    Type = RoleTypeAttribute.View,
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });

                context.Logs.Add(new Log
                {
                    Id = Guid.NewGuid(),
                    Action = LogActionsAttribute.UserCreated,
                    Type = LogTypeAttribute.Information,
                    LogMessage = "Created the Admin user from OnModelCreating",
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });

                context.SystemSettings.Add(new SystemSettings
                {
                    EmailConfirmationTokenExpiredAtHours = 24,
                    ForgotPasswordTokenExpiredAtHours = 1,
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });

                string bodyEmailConfirmation = @"
                            <h2>Здравствуйте, {{UserName}}!</h2>
                            <p>
                                Подтвердите ваш email, перейдя по ссылке:
                                <a href=""{{ConfirmationLink}}"">Подтвердить</a>
                            </p>";

                context.EmailTemplates.Add(new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = EmailTemplateNameAttribute.EmailConfirmation,
                    Subject = "Подтверждение регистрации",
                    BodyHtml = bodyEmailConfirmation,
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });

                string bodyForgotPassword = @"<h2>Здравствуйте, {{UserName}}!</h2>
                                        <p>
                                            Вы запросили сброс пароля для вашей учетной записи.  
                                            Чтобы создать новый пароль, перейдите по ссылке ниже:
                                        </p>
                                        <p>
                                            <a href=""{ { ConfirmationLink } }"" style=""display: inline - block; padding: 10px 20px;
                                                    background-color:#0d6efd;color:#fff;text-decoration:none;border-radius:5px;"">
                                               Сбросить пароль
                                            </a>
                                        </p>
                                        <p>
                                            Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.
                                        </p>";
                context.EmailTemplates.Add(new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = EmailTemplateNameAttribute.ForgotPassword,
                    Subject = "Сброс пароля",
                    BodyHtml = bodyForgotPassword,
                    IsDeleted = false,
                    CreatedBy = adminUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = adminUserId,
                    UpdatedDate = DateTime.UtcNow,
                });

                context.SaveChanges();
            }
        }
    }
}
