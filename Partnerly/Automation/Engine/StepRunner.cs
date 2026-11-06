using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Automation.Registry;

namespace Partnerly.Automation.Engine
{
    public class StepRunner
    {
        private readonly ActionRegistry _actions;
        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;

        public StepRunner(ActionRegistry actions, IAutomationStore store, IAutomationClock clock)
        {
            _actions = actions;
            _store = store;
            _clock = clock;
        }

        public async Task<StepResult> RunAsync(
            ActionContext context,
            IReadOnlyList<AutomationRunStep> existing,
            int retryLimit,
            CancellationToken cancellationToken,
            bool repeated = false)
        {
            // repeated: the card runs again on purpose (next item of a "For each"), so an earlier success
            // must not short-circuit it.
            var succeeded = !repeated && existing.Any(step =>
                step.NodeId == context.Node.Id && step.Status == AutomationStatuses.StepSucceeded);
            if (succeeded)
            {
                return StepResult.Succeeded();
            }

            var attempt = existing.Count(step => step.NodeId == context.Node.Id) + 1;
            var handler = _actions.Resolve(context.Services, context.Node.Type);
            var started = _clock.UtcNow;
            StepResult result;
            try
            {
                result = await handler.ExecuteAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                result = StepResult.Failed(ex.Message);
            }

            await _store.AddStepAsync(new AutomationRunStep
            {
                Id = Guid.NewGuid(),
                RunId = context.Run.Id,
                NodeId = context.Node.Id,
                ActionType = context.Node.Type,
                Status = result.Status,
                Attempt = attempt,
                InputJson = context.Node.Config.ValueKind == JsonValueKind.Undefined
                    ? null
                    : context.Node.Config.GetRawText(),
                OutputJson = result.Output?.ToJsonString(),
                Error = result.Error,
                StartedAt = started,
                EndedAt = _clock.UtcNow
            }, cancellationToken);

            var retryAttempt = attempt;
            if (repeated)
            {
                // count only the failures after the last success of this card in this pass
                var own = existing.Where(step => step.NodeId == context.Node.Id).OrderBy(step => step.Attempt).ToList();
                var lastOk = own.FindLastIndex(step => step.Status != AutomationStatuses.StepFailed);
                retryAttempt = own.Count - (lastOk + 1) + 1;
            }

            if (result.Status == AutomationStatuses.StepFailed && result.Retryable && retryAttempt < retryLimit)
            {
                result.WakeupKind = AutomationStatuses.WakeupRetry;
                var delaySeconds = Math.Min(60, 1 << Math.Min(retryAttempt - 1, 6));
                result.ResumeAtUtc = _clock.UtcNow.AddSeconds(delaySeconds);
            }

            return result;
        }
    }
}
