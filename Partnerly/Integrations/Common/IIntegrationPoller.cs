using System.Text.Json;

namespace Partnerly.Integrations.Common
{
    // One poller per trigger type. The worker hands it a connection plus the
    // configs of every published trigger bound to that connection.
    public interface IIntegrationPoller
    {
        string TriggerType { get; }
        string ConnectionKind { get; }
        TimeSpan Interval { get; }

        Task PollAsync(IntegrationPollTarget target, CancellationToken cancellationToken);
    }

    public class IntegrationPollTarget
    {
        public IntegrationPollTarget(IntegrationConnection connection, IReadOnlyList<JsonElement> triggerConfigs)
        {
            Connection = connection;
            TriggerConfigs = triggerConfigs;
        }

        public IntegrationConnection Connection { get; }
        public IReadOnlyList<JsonElement> TriggerConfigs { get; }
    }
}
