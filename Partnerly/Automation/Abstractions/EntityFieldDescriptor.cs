namespace Partnerly.Automation.Abstractions
{
    public class EntityFieldDescriptor
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
        public bool Filterable { get; set; } = true;
        public bool Updatable { get; set; }
    }
}
