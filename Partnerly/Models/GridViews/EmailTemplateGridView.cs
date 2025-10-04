using Partnerly.Models.ViewModels;

namespace Partnerly.Models.GridViews
{
    public class EmailTemplateGridView : EmailTemplateViewModel
    {
        public bool? Select { get; set; }
        public string? CreatedByUserFullName { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
