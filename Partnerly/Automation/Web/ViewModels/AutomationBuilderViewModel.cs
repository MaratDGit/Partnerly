namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationBuilderViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GraphJson { get; set; } = "{}";
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
    }
}
