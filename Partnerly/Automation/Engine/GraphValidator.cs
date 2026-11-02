using System.Text.RegularExpressions;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Engine
{
    public class GraphValidator
    {
        private static readonly Regex NodeName = new(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public IReadOnlyList<string> Validate(WorkflowGraph graph)
        {
            var errors = new List<string>();
            if (graph.SchemaVersion != 1)
            {
                errors.Add("Unsupported graph schemaVersion.");
            }

            var triggers = graph.Nodes.Where(node => node.Kind == "trigger").ToList();
            if (triggers.Count == 0)
            {
                errors.Add("Graph must have at least one trigger.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id) || !ids.Add(node.Id))
                {
                    errors.Add("Each node needs a unique id.");
                }

                if (string.IsNullOrWhiteSpace(node.Name) || !NodeName.IsMatch(node.Name) || !names.Add(node.Name))
                {
                    errors.Add($"Invalid or duplicate node name '{node.Name}'.");
                }

                if (node.Kind == "trigger")
                {
                    if (!AutomationTypeCodes.Triggers.Contains(node.Type))
                    {
                        errors.Add($"Unknown trigger type '{node.Type}'.");
                    }
                }
                else if (node.Kind == "action")
                {
                    if (!AutomationTypeCodes.Actions.Contains(node.Type))
                    {
                        errors.Add($"Unknown action type '{node.Type}'.");
                    }
                }
                else
                {
                    errors.Add($"Unknown node kind '{node.Kind}'.");
                }
            }

            var nodeIds = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var edge in graph.Edges)
            {
                if (!nodeIds.Contains(edge.From) || !nodeIds.Contains(edge.To))
                {
                    errors.Add($"Edge '{edge.Id}' points to a missing node.");
                }
            }

            foreach (var node in graph.Nodes.Where(item => item.Type == AutomationTypeCodes.Branch))
            {
                var ports = graph.Edges.Where(edge => edge.From == node.Id).Select(edge => edge.Port).ToList();
                if (!ports.Contains("true") || !ports.Contains("false"))
                {
                    errors.Add($"Branch '{node.Name}' must have true and false ports.");
                }
            }

            foreach (var node in graph.Nodes.Where(item => item.Type == AutomationTypeCodes.Switch))
            {
                var ports = graph.Edges.Where(edge => edge.From == node.Id).Select(edge => edge.Port).ToList();
                if (!ports.Contains("default") && !ports.Any(port => port != null && port.StartsWith("case:", StringComparison.Ordinal)))
                {
                    errors.Add($"Switch '{node.Name}' needs case or default ports.");
                }
            }

            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>(triggers.Select(item => item.Id));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!reachable.Add(current))
                {
                    continue;
                }

                foreach (var edge in graph.Edges.Where(item => item.From == current))
                {
                    queue.Enqueue(edge.To);
                }
            }

            foreach (var node in graph.Nodes.Where(item => item.Kind == "action"))
            {
                if (!reachable.Contains(node.Id))
                {
                    errors.Add($"Action '{node.Name}' is not reachable from a trigger.");
                }
            }

            return errors;
        }
    }
}
