using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class LogGridViewModel : LogsViewModel
    {
        public bool? Select { get; set; }
        public string? CreatorUserName { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
