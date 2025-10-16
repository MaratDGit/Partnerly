using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class UserGroupGridViewModel : UserGroupViewModel
    {
        public bool? Select { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
