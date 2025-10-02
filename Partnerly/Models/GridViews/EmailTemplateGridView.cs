namespace Partnerly.Models.GridViews
{
    public class EmailTemplateGridView : EmailTemplate
    {
        public string? CreatedByUserFullName { get; set; }
        public List<GridAction>? Actions { get; set; }
    }
}
