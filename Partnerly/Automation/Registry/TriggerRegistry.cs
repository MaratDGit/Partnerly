using Partnerly.Automation.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Partnerly.Automation.Registry
{
    public class TriggerRegistry
    {
        private readonly Dictionary<string, Type> _handlers = new(StringComparer.Ordinal);

        public void Register<THandler>(string type) where THandler : class, ITriggerHandler
        {
            _handlers[type] = typeof(THandler);
        }

        public bool IsKnown(string type) => _handlers.ContainsKey(type);

        public ITriggerHandler Resolve(IServiceProvider services, string type)
        {
            if (!_handlers.TryGetValue(type, out var handlerType))
            {
                throw new InvalidOperationException($"Unknown trigger '{type}'.");
            }

            return (ITriggerHandler)services.GetRequiredService(handlerType);
        }

        public IReadOnlyCollection<string> Codes => _handlers.Keys.ToList();

        public IReadOnlyList<HandlerDescriptor> DescribeAll(IServiceProvider services) =>
            _handlers.Values
                .Select(type => ((ITriggerHandler)services.GetRequiredService(type)).Describe())
                .ToList();
    }
}
