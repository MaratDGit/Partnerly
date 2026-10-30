using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Messages
    {
        private static readonly ResourceManager ResourceManager =
        new ResourceManager(
            "Partnerly.Resources.SharedResource",
            typeof(SharedResource).Assembly);

        public static string Get(string key)
        {
            return ResourceManager.GetString(
                key,
                CultureInfo.CurrentUICulture) ?? key;
        }

        public static string RecordSaved => Get(nameof(RecordSaved));
        public static string SupportSuccess => Get(nameof(SupportSuccess));
        public static string CaseCreationLimit => Get(nameof(CaseCreationLimit));
        public static string SiteUnderMaintenanceMode => Get(nameof(SiteUnderMaintenanceMode));
        public static string UserCannotBeFound => Get(nameof(UserCannotBeFound));

        public static string UserRegistrationNotification => Get(nameof(UserRegistrationNotification));
        public static string NewTicketToEmployeeNotification => Get(nameof(NewTicketToEmployeeNotification));
        public static string TicketStartedNotification => Get(nameof(TicketStartedNotification));
        public static string TicketReopenedNotification => Get(nameof(TicketReopenedNotification));
        public static string TicketClosedNotification => Get(nameof(TicketClosedNotification));
        public static string GroupMembersSent => Get(nameof(GroupMembersSent));
        public static string AdminNewRegistrationSubject => Get(nameof(AdminNewRegistrationSubject));
        public static string AdminNewRegistrationBody => Get(nameof(AdminNewRegistrationBody));
        public static string AdminNewTicketSubject => Get(nameof(AdminNewTicketSubject));
        public static string AdminNewTicketBody => Get(nameof(AdminNewTicketBody));
    }
}
