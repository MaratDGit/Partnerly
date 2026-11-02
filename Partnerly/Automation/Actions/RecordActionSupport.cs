using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Registry;

namespace Partnerly.Automation.Actions
{
    internal static class RecordActionSupport
    {
        public static IEntityConnector? Connector(ActionContext context, out string? error)
        {
            var key = NodeConfig.GetString(context.Node, "entity");
            if (string.IsNullOrWhiteSpace(key))
            {
                error = "Entity is required.";
                return null;
            }

            var registry = context.Services.GetService(typeof(EntityRegistry)) as EntityRegistry;
            var connector = registry?.Get(key);
            if (connector == null)
            {
                error = $"Unknown entity '{key}'.";
                return null;
            }

            error = null;
            return connector;
        }

        public static JsonObject ReadObject(WorkflowNode node, IVariableBinder binder, JsonObject variables, string name)
        {
            var result = new JsonObject();
            if (node.Config.ValueKind != JsonValueKind.Object
                || !node.Config.TryGetProperty(name, out var value))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var field = item.TryGetProperty("field", out var fieldNode) ? fieldNode.GetString() : null;
                    var raw = item.TryGetProperty("value", out var valueNode) ? valueNode.ToString().Trim('"') : null;
                    if (!string.IsNullOrEmpty(field))
                    {
                        result[field] = binder.Interpolate(raw, variables);
                    }
                }

                return result;
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in value.EnumerateObject())
                {
                    result[property.Name] = binder.Interpolate(property.Value.ToString().Trim('"'), variables);
                }
            }

            return result;
        }
    }
}
