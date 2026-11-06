using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;
using Partnerly.Integrations.Common;

namespace Partnerly.Computer
{
    public class ComputerResponse
    {
        public bool Ok { get; set; }
        public JsonObject Data { get; set; } = new();
        public string? Error { get; set; }

        // Set when the text comes from the site itself (not from the PC): it can then be shown in the owner's language.
        public string? ErrorKey { get; set; }
    }

    // Sends a command to the Partnerly Agent of one computer and waits for its answer.
    public interface IComputerGateway
    {
        bool IsOnline(Guid connectionId);

        Task<ComputerResponse> SendAsync(Guid connectionId, string op, JsonObject args, TimeSpan timeout, CancellationToken cancellationToken);
    }

    public sealed class ComputerGateway : IComputerGateway
    {
        private sealed class Pending
        {
            public Guid ConnectionId { get; init; }
            public TaskCompletionSource<ComputerResponse> Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly IHubContext<ComputerHub> _hub;
        private readonly ConcurrentDictionary<Guid, string> _online = new();
        private readonly ConcurrentDictionary<string, Pending> _pending = new();

        public ComputerGateway(IHubContext<ComputerHub> hub)
        {
            _hub = hub;
        }

        public bool IsOnline(Guid connectionId) => _online.ContainsKey(connectionId);

        private const int EventsPerMinute = 120;
        private readonly ConcurrentDictionary<Guid, (DateTime Start, int Count)> _events = new();

        // A PC that floods events (a folder full of new files) must not flood the agents.
        public bool AllowEvent(Guid connectionId)
        {
            if (connectionId == Guid.Empty)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var state = _events.AddOrUpdate(connectionId,
                _ => (now, 1),
                (_, old) => now - old.Start > TimeSpan.FromMinutes(1) ? (now, 1) : (old.Start, old.Count + 1));
            return state.Count <= EventsPerMinute;
        }

        // The newest connection of a computer wins; the older one is told to leave.
        public async Task AttachAsync(Guid connectionId, string signalRId)
        {
            if (_online.TryGetValue(connectionId, out var previous) && previous != signalRId)
            {
                await _hub.Clients.Client(previous).SendAsync("Replaced");
            }

            _online[connectionId] = signalRId;
        }

        // False when this SignalR connection was already replaced by a newer one.
        public bool Detach(Guid connectionId, string signalRId)
        {
            if (!_online.TryGetValue(connectionId, out var current) || current != signalRId)
            {
                return false;
            }

            _online.TryRemove(new KeyValuePair<Guid, string>(connectionId, signalRId));
            foreach (var item in _pending.Where(p => p.Value.ConnectionId == connectionId).ToList())
            {
                if (_pending.TryRemove(item.Key, out var pending))
                {
                    pending.Source.TrySetResult(new ComputerResponse { Error = "Computer went offline while the command was running.", ErrorKey = "ComputerErrWentOffline" });
                }
            }

            return true;
        }

        // Only the computer the command was sent to may answer it.
        public bool Complete(Guid connectionId, string commandId, ComputerResponse response)
        {
            if (_pending.TryGetValue(commandId, out var pending) && pending.ConnectionId == connectionId
                && _pending.TryRemove(commandId, out _))
            {
                pending.Source.TrySetResult(response);
                return true;
            }

            return false;
        }

        public async Task<ComputerResponse> SendAsync(Guid connectionId, string op, JsonObject args, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!_online.TryGetValue(connectionId, out var signalRId))
            {
                throw new ComputerException("ComputerErrOffline", "The computer is offline. Start Partnerly Agent on it and try again.");
            }

            var commandId = Guid.NewGuid().ToString("N");
            var pending = new Pending { ConnectionId = connectionId };
            _pending[commandId] = pending;
            try
            {
                await _hub.Clients.Client(signalRId).SendAsync("Execute", commandId, op, args.ToJsonString(), cancellationToken);
                var finished = await Task.WhenAny(pending.Source.Task, Task.Delay(timeout, cancellationToken));
                if (finished != pending.Source.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new ComputerException("ComputerErrTimeout", "The computer did not answer in time.");
                }

                return await pending.Source.Task;
            }
            finally
            {
                _pending.TryRemove(commandId, out _);
            }
        }
    }
}

namespace Partnerly.Computer
{
    // An error that comes from the site's own checks; Key names the translated text.
    public sealed class ComputerException : IntegrationException
    {
        public ComputerException(string key, string message) : base(message)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
