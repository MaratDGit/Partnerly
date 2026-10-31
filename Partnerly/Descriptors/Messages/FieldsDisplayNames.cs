using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class FieldsDisplayNames
    {
        public static string Email => Messages.Get(nameof(Email));
        public static string Password => Messages.Get(nameof(Password));
        public static string NewPassword => Messages.Get(nameof(NewPassword));
        public static string ConfirmPassword => Messages.Get(nameof(ConfirmPassword));
        public static string RememberMe => Messages.Get(nameof(RememberMe));
        public static string Phone => Messages.Get(nameof(Phone));
        public static string FirstName => Messages.Get(nameof(FirstName));
        public static string LastName => Messages.Get(nameof(LastName));
        public static string FullName => Messages.Get(nameof(FullName));
        public static string ReferrerCode => Messages.Get(nameof(ReferrerCode));
        public static string ReferrerName => Messages.Get(nameof(ReferrerName));
        public static string TermsConditions => Messages.Get(nameof(TermsConditions));
        public static string Subject => Messages.Get(nameof(Subject));
        public static string BodyHTML => Messages.Get(nameof(BodyHTML));
        public static string Balance => Messages.Get(nameof(Balance));
        public static string LastActivity => Messages.Get(nameof(LastActivity));
        public static string IsOnline => Messages.Get(nameof(IsOnline));
        public static string IsBlocked => Messages.Get(nameof(IsBlocked));
        public static string EmailConfirmed => Messages.Get(nameof(EmailConfirmed));
        public static string Role => Messages.Get(nameof(Role));
        public static string Roles => Messages.Get(nameof(Roles));
        public static string NewRole => Messages.Get(nameof(NewRole));
        public static string EditRole => Messages.Get(nameof(EditRole));
        public static string EditUserRole => Messages.Get(nameof(EditUserRole));
        public static string UserRole => Messages.Get(nameof(UserRole));
        public static string History => Messages.Get(nameof(History));
        public static string PerformedBy => Messages.Get(nameof(PerformedBy));
        public static string UserRoleChanged => Messages.Get(nameof(UserRoleChanged));
        public static string UserPermissionAdded => Messages.Get(nameof(UserPermissionAdded));
        public static string UserPermissionRemoved => Messages.Get(nameof(UserPermissionRemoved));
        public static string UserPermissionGrantUpdated => Messages.Get(nameof(UserPermissionGrantUpdated)); 
        public static string TemplateType => Messages.Get(nameof(TemplateType));
        public static string AllowSendNotifications => Messages.Get(nameof(AllowSendNotifications));
        public static string AllowSendEmails => Messages.Get(nameof(AllowSendEmails));

        public static string Edit => Messages.Get(nameof(Edit));
        public static string Delete => Messages.Get(nameof(Delete));
        public static string Details => Messages.Get(nameof(Details));

        public static string CreatedDate => Messages.Get(nameof(CreatedDate));
        public static string CreatorName => Messages.Get(nameof(CreatorName));
        public static string UpdatedDate => Messages.Get(nameof(UpdatedDate));
        public static string UpdaterName => Messages.Get(nameof(UpdaterName));

        public static string MaintenanceMode => Messages.Get(nameof(MaintenanceMode));
        public static string EmailConfirmationTokenHours => Messages.Get(nameof(EmailConfirmationTokenHours));
        public static string ForgotPasswordTokenExpiredAtHours => Messages.Get(nameof(ForgotPasswordTokenExpiredAtHours));
        public static string Automaticonlinestatusupdate => Messages.Get(nameof(Automaticonlinestatusupdate));
        public static string ScrollingText => Messages.Get(nameof(ScrollingText));
        public static string ApplicationName => Messages.Get(nameof(ApplicationName));
        public static string MaintenanceMessage => Messages.Get(nameof(MaintenanceMessage));
        public static string DefaultLanguage => Messages.Get(nameof(DefaultLanguage));
        public static string TimeZone => Messages.Get(nameof(TimeZone));
        public static string AiAssistantEnabled => Messages.Get(nameof(AiAssistantEnabled));
        public static string DefaultNewUserRole => Messages.Get(nameof(DefaultNewUserRole));
        public static string EnableGoogleLogin => Messages.Get(nameof(EnableGoogleLogin));
        public static string PersistentCookieDays => Messages.Get(nameof(PersistentCookieDays));
        public static string SessionExpireMinutes => Messages.Get(nameof(SessionExpireMinutes));
        public static string MaxProfilePhotoSizeMb => Messages.Get(nameof(MaxProfilePhotoSizeMb));
        public static string MaxFailedLoginAttempts => Messages.Get(nameof(MaxFailedLoginAttempts));
        public static string LockoutDurationMinutes => Messages.Get(nameof(LockoutDurationMinutes));
        public static string EnableEmailSending => Messages.Get(nameof(EnableEmailSending));
        public static string EnableNotifications => Messages.Get(nameof(EnableNotifications));
        public static string EmailAdminOnNewRegistration => Messages.Get(nameof(EmailAdminOnNewRegistration));
        public static string EmailAdminOnNewTicket => Messages.Get(nameof(EmailAdminOnNewTicket));
        public static string AdminNotificationEmail => Messages.Get(nameof(AdminNotificationEmail));
        public static string DefaultAllowSendEmails => Messages.Get(nameof(DefaultAllowSendEmails));
        public static string DefaultAllowSendNotifications => Messages.Get(nameof(DefaultAllowSendNotifications));
        public static string MaxAttachmentSizeMb => Messages.Get(nameof(MaxAttachmentSizeMb));
        public static string AllowedAttachmentExtensions => Messages.Get(nameof(AllowedAttachmentExtensions));
        public static string TicketAutoAssignUser => Messages.Get(nameof(TicketAutoAssignUser));
        public static string TicketAutoAssignRole => Messages.Get(nameof(TicketAutoAssignRole));

        public static string Save => Messages.Get(nameof(Save));
        public static string Create => Messages.Get(nameof(Create));
        public static string More => Messages.Get(nameof(More));
        public static string Back => Messages.Get(nameof(Back));
        public static string Action => Messages.Get(nameof(Action));
        public static string Type => Messages.Get(nameof(Type));
        public static string Message => Messages.Get(nameof(Message));
        public static string FilePath => Messages.Get(nameof(FilePath));
        public static string Method => Messages.Get(nameof(Method));
        public static string LineNumber => Messages.Get(nameof(LineNumber));

        public static string Admin => Messages.Get(nameof(Admin));
        public static string Employee => Messages.Get(nameof(Employee));
        public static string User => Messages.Get(nameof(User));

        public static string Name => Messages.Get(nameof(Name));
        public static string Description => Messages.Get(nameof(Description));
        public static string Group => Messages.Get(nameof(Group));
        public static string IsRead => Messages.Get(nameof(IsRead));
        public static string Status => Messages.Get(nameof(Status));
        public static string AssignedTo => Messages.Get(nameof(AssignedTo));
        public static string TaskNumber => Messages.Get(nameof(TaskNumber));
        public static string Viewcase => Messages.Get(nameof(Viewcase));

        public static string Closed => Messages.Get(nameof(Closed));
        public static string InProgress => Messages.Get(nameof(InProgress));
        public static string New => Messages.Get(nameof(New));

        public static string Open => Messages.Get(nameof(Open));
        public static string Pending => Messages.Get(nameof(Pending));
        public static string Paid => Messages.Get(nameof(Paid));
        public static string Canceled => Messages.Get(nameof(Canceled));
        public static string Rejected => Messages.Get(nameof(Rejected));

        public static string UserSignIn => Messages.Get(nameof(UserSignIn));
        public static string UserSignOut => Messages.Get(nameof(UserSignOut));

        public static string UserCreated => Messages.Get(nameof(UserCreated));
        public static string UserDeleted => Messages.Get(nameof(UserDeleted));
        public static string UserUpdated => Messages.Get(nameof(UserUpdated));

        public static string RoleCreated => Messages.Get(nameof(RoleCreated));
        public static string RoleDeleted => Messages.Get(nameof(RoleDeleted));
        public static string RoleUpdated => Messages.Get(nameof(RoleUpdated));

        public static string PaymentCreated => Messages.Get(nameof(PaymentCreated));
        public static string PaymentDeleted => Messages.Get(nameof(PaymentDeleted));
        public static string PaymentUpdated => Messages.Get(nameof(PaymentUpdated));

        public static string SystemSettingsCreated => Messages.Get(nameof(SystemSettingsCreated));
        public static string SystemSettingsDeleted => Messages.Get(nameof(SystemSettingsDeleted));
        public static string SystemSettingsUpdated => Messages.Get(nameof(SystemSettingsUpdated));

        public static string EmailConfirmationTokenCreated => Messages.Get(nameof(EmailConfirmationTokenCreated));
        public static string EmailConfirmationTokenDeleted => Messages.Get(nameof(EmailConfirmationTokenDeleted));
        public static string EmailConfirmationTokenUpdated => Messages.Get(nameof(EmailConfirmationTokenUpdated));

        public static string EmailTemplateCreated => Messages.Get(nameof(EmailTemplateCreated));
        public static string EmailTemplateDeleted => Messages.Get(nameof(EmailTemplateDeleted));
        public static string EmailTemplateUpdated => Messages.Get(nameof(EmailTemplateUpdated));

        public static string NotificationCreated => Messages.Get(nameof(NotificationCreated));
        public static string NotificationDeleted => Messages.Get(nameof(NotificationDeleted));
        public static string NotificationUpdated => Messages.Get(nameof(NotificationUpdated));

        public static string UserGroupCreated => Messages.Get(nameof(UserGroupCreated));
        public static string UserGroupDeleted => Messages.Get(nameof(UserGroupDeleted));
        public static string UserGroupUpdated => Messages.Get(nameof(UserGroupUpdated));

        public static string SupportTicketCreated => Messages.Get(nameof(SupportTicketCreated));
        public static string SupportTicketDeleted => Messages.Get(nameof(SupportTicketDeleted));
        public static string SupportTicketUpdated => Messages.Get(nameof(SupportTicketUpdated));

        public static string EmailSending => Messages.Get(nameof(EmailSending));

        public static string Information => Messages.Get(nameof(Information));
        public static string Warning => Messages.Get(nameof(Warning));
        public static string Error => Messages.Get(nameof(Error));
        public static string Critical => Messages.Get(nameof(Critical));

        public static string Success => Messages.Get(nameof(Success));

        public static string EmailConfirmation => Messages.Get(nameof(EmailConfirmation));

        public static string Registration => Messages.Get(nameof(Registration));
        public static string ForgotPassword => Messages.Get(nameof(ForgotPassword));
        public static string CriticalError => Messages.Get(nameof(CriticalError));
        public static string Link => Messages.Get(nameof(Link));
        public static string Owner => Messages.Get(nameof(Owner));
        public static string FileName => Messages.Get(nameof(FileName));
        public static string StoredFileName => Messages.Get(nameof(StoredFileName));
        public static string ContentType => Messages.Get(nameof(ContentType));
        public static string FileSize => Messages.Get(nameof(FileSize));
        public static string StoragePath => Messages.Get(nameof(StoragePath));
        public static string EntityType => Messages.Get(nameof(EntityType));
        public static string Entity => Messages.Get(nameof(Entity));
        public static string IsDeleted => Messages.Get(nameof(IsDeleted));
        public static string Photo => Messages.Get(nameof(Photo));

        public static string DataTableSearch => Messages.Get(nameof(DataTableSearch));
        public static string Users => Messages.Get(nameof(Users));

        public static string View => Messages.Get(nameof(View));
        public static string Attachments => Messages.Get(nameof(Attachments));
        public static string Reload => Messages.Get(nameof(Reload));
        public static string Add => Messages.Get(nameof(Add));
        public static string Templates => Messages.Get(nameof(Templates));
        public static string Tasks => Messages.Get(nameof(Tasks));
        public static string Logs => Messages.Get(nameof(Logs));
        public static string Notifications => Messages.Get(nameof(Notifications));

        public static string Sendanemail => Messages.Get(nameof(Sendanemail));
        public static string Sendnotification => Messages.Get(nameof(Sendnotification));
        public static string Viewuser => Messages.Get(nameof(Viewuser));
        public static string Preview => Messages.Get(nameof(Preview));
        public static string IsActive => Messages.Get(nameof(IsActive));
        public static string Permissions => Messages.Get(nameof(Permissions));
        public static string Permission => Messages.Get(nameof(Permission));
        public static string IsGranted => Messages.Get(nameof(IsGranted));
        public static string Module => Messages.Get(nameof(Module));
    }
}
