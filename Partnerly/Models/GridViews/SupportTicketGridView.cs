using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class SupportTicketGridView : SupportTicketViewModel
    {
        public bool? Select { get; set; }
        public string? AssignedToUserName { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
