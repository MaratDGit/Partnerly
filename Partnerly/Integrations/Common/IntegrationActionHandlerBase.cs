using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Integrations.Common
{
    // Base for every integration action. Resolves the node's connectionId,
    // checks it belongs to the run owner and has the right Kind (IDOR guard),
    // and skips writes in dry-run like SideEffectActionHandlerBase does.
    public abstract class IntegrationActionHandlerBase : IActionHandler
    {
        public abstract string Type { get; }
        public abstract string DisplayName { get; }
        public abstract string Group { get; }
        public abstract string ConnectionKind { get; }

        // Read-only actions (find_*) still run in a test run.
        protected virtual bool Writes => true;

        public bool HasSideEffects => Writes;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = DisplayName,
            HasSideEffects = Writes,
            Group = Group,
            ConnectionKind = ConnectionKind
        };

        public async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (context.IsDryRun && Writes)
            {
                return StepResult.Skipped("dryRun");
            }

            var binder = context.Services.GetRequiredService<IVariableBinder>();
            var raw = binder.Interpolate(NodeConfig.GetString(context.Node, "connectionId"), context.Variables);
            if (!Guid.TryParse(raw, out var connectionId))
            {
                return StepResult.Failed("Choose a connection for this step.", retryable: false);
            }

            var connections = context.Services.GetRequiredService<IIntegrationConnectionService>();
            var connection = await connections.GetForOwnerAsync(connectionId, context.Run.OwnerUserId, ConnectionKind, cancellationToken);
            if (connection == null)
            {
                return StepResult.Failed("Connection was not found.", retryable: false);
            }

            var clock = context.Services.GetRequiredService<IAutomationClock>();
            try
            {
                var result = await ExecuteCoreAsync(context, connection, binder, cancellationToken);
                if (result.Status == AutomationStatuses.StepSucceeded)
                {
                    // Also persists refreshed Google tokens written into the payload.
                    connection.MarkOk(clock.UtcNow);
                    await connections.SaveAsync(connection, cancellationToken);
                }

                return result;
            }
            catch (IntegrationException ex)
            {
                connection.MarkFailed(clock.UtcNow, ex.Message);
                await connections.SaveAsync(connection, cancellationToken);
                return StepResult.Failed(ex.Message, ex.Retryable);
            }
            catch (HttpRequestException ex)
            {
                return StepResult.Failed("Service is not reachable: " + ex.Message);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StepResult.Failed("Service did not answer in time.");
            }
        }

        protected abstract Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken);

        protected static string Read(ActionContext context, IVariableBinder binder, string key) =>
            binder.Interpolate(NodeConfig.GetString(context.Node, key), context.Variables).Trim();
    }
}
