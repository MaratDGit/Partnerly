using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class UserGridViewModel : UserViewModel
    {
        public string? UserName { get; set; }
        public string? ReferrerName { get; set; }
        public string? RoleName { get; set; }

        public List<GridAction>? Actions { get; set; }
    }
}
