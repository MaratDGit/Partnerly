using Partnerly.Automation.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Partnerly.Automation.Registry
{
    public class ActionRegistry
    {
        private readonly Dictionary<string, Type> _handlers = new(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _connectionKinds = new(StringComparer.Ordinal);

        // connectionKind: the node must carry config.connectionId of that kind (integration steps).
        public void Register<THandler>(string type, string? connectionKind = null) where THandler : class, IActionHandler
        {
            _handlers[type] = typeof(THandler);
            if (connectionKind != null)
            {
                _connectionKinds[type] = connectionKind;
            }
        }

        public bool IsKnown(string type) => _handlers.ContainsKey(type);

        public string? ConnectionKindOf(string type) =>
            _connectionKinds.TryGetValue(type, out var kind) ? kind : null;

        public IActionHandler Resolve(IServiceProvider services, string type)
        {
            if (!_handlers.TryGetValue(type, out var handlerType))
            {
                throw new InvalidOperationException($"Unknown action '{type}'.");
            }

            return (IActionHandler)services.GetRequiredService(handlerType);
        }

        public IReadOnlyCollection<string> Codes => _handlers.Keys.ToList();

        public IReadOnlyList<HandlerDescriptor> DescribeAll(IServiceProvider services) =>
            _handlers.Values
                .Select(type => ((IActionHandler)services.GetRequiredService(type)).Describe())
                .ToList();
    }
}
