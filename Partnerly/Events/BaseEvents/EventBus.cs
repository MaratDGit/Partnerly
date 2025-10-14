namespace Partnerly.Events.BaseEvents
{
    public class EventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, List<Type>> _handlers = new();

        public EventBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Subscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);

            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<Type>();

            _handlers[eventType].Add(handlerType);
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType)) return;

            foreach (var handlerType in _handlers[eventType])
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = (IEventHandler<TEvent>)scope.ServiceProvider.GetRequiredService(handlerType);
                await handler.HandleAsync(@event);
            }
        }
    }
}
