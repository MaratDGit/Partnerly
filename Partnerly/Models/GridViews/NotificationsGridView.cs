using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class NotificationsGridView : NotificationsViewModel
    {
        public bool? Select { get; set; }
        public string? CreatedByUserFullName { get; set; }
        public string? OwnerUserFullName { get; set; }
        public string? TicketNumber { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
