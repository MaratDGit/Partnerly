using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationToolsViewModel
    {
        public AutomationDefinition Definition { get; set; } = null!;
        public List<ToolsVersionRow> Versions { get; } = new();
    }

    public class ToolsVersionRow
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public int Cards { get; set; }
        public bool IsDraft { get; set; }
        public bool IsLive { get; set; }
        public string WhenText { get; set; } = string.Empty;
    }

    public class ToolsDayRow
    {
        public string Label { get; set; } = string.Empty;
        public int Ok { get; set; }
        public int Failed { get; set; }
        public int Other { get; set; }
    }
}
