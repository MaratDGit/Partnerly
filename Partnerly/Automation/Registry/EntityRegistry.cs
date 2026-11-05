using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Registry
{
    public class EntityRegistry
    {
        private readonly Dictionary<string, IEntityConnector> _connectors;

        public EntityRegistry(IEnumerable<IEntityConnector> connectors)
        {
            _connectors = connectors.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<IEntityConnector> All => _connectors.Values;

        public IEntityConnector? Get(string key) =>
            _connectors.TryGetValue(key, out var connector) ? connector : null;

        public JsonObject FilterBag(JsonObject bag)
        {
            var binder = new Engine.VariableBinder();
            var filtered = new JsonObject();
            foreach (var pair in bag)
            {
                if (binder.IsBlockedPath(pair.Key))
                {
                    continue;
                }

                filtered[pair.Key] = Engine.JsonNodes.Clone(pair.Value);
            }

            return filtered;
        }
    }
}
