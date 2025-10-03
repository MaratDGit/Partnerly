using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class EmailTemplateGridView : EmailTemplateViewModel
    {
        public string? CreatedByUserFullName { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
