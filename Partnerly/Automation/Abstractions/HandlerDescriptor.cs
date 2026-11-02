namespace Partnerly.Automation.Abstractions
{
    public class HandlerDescriptor
    {
        public string Type { get; set; } = string.Empty;
        public string Kind { get; set; } = "action";
        public string DisplayName { get; set; } = string.Empty;
        public bool HasSideEffects { get; set; }
    }
}
