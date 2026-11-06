using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Actions
{
    // Shared helpers for the data store cards (key / value that survives between runs of one agent).
    public abstract class DataStoreActionHandlerBase : IActionHandler
    {
        public const int MaxKeyLength = 190;
        public const int MaxValueLength = 20000;

        protected readonly IVariableBinder Binder;

        protected DataStoreActionHandlerBase(IVariableBinder binder)
        {
            Binder = binder;
        }

        public abstract string Type { get; }
        public abstract bool HasSideEffects { get; }
        protected abstract string DisplayName { get; }
        protected virtual string KeyPrefix => string.Empty;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = DisplayName,
            HasSideEffects = HasSideEffects
        };

        public abstract Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken);

        protected bool TryKey(ActionContext context, out string key, out StepResult? failure)
        {
            key = Binder.Interpolate(NodeConfig.GetString(context.Node, "key"), context.Variables).Trim();
            failure = null;
            if (key.Length == 0 || key.Length > MaxKeyLength)
            {
                failure = StepResult.Failed("The key is empty or too long.", retryable: false);
                return false;
            }

            key = KeyPrefix + key;
            return true;
        }

        protected static DateTime? Expiry(ActionContext context)
        {
            return double.TryParse(NodeConfig.GetString(context.Node, "ttlHours"), System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture, out var hours) && hours > 0
                ? context.UtcNow.AddHours(Math.Min(hours, 24 * 365))
                : null;
        }

        protected static IAutomationStore? StoreOf(ActionContext context) =>
            context.Services.GetService(typeof(IAutomationStore)) as IAutomationStore;
    }

    public class StoreSetActionHandler : DataStoreActionHandlerBase
    {
        public StoreSetActionHandler(IVariableBinder binder) : base(binder) { }

        public override string Type => AutomationTypeCodes.StoreSet;
        public override bool HasSideEffects => true;
        protected override string DisplayName => "Save a value";

        public override async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!TryKey(context, out var key, out var failure))
            {
                return failure!;
            }

            var value = Binder.Interpolate(NodeConfig.GetString(context.Node, "value"), context.Variables);
            if (value.Length > MaxValueLength)
            {
                return StepResult.Failed("The value is too long.", retryable: false);
            }

            if (context.IsDryRun)
            {
                return StepResult.Skipped("dryRun");
            }

            var store = StoreOf(context);
            if (store == null)
            {
                return StepResult.Failed("Automation services are not available.", retryable: false);
            }

            await store.SetDataItemAsync(context.Run.AutomationId, key, value, context.UtcNow, Expiry(context), cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["key"] = key, ["value"] = value });
        }
    }

    public class StoreGetActionHandler : DataStoreActionHandlerBase
    {
        public StoreGetActionHandler(IVariableBinder binder) : base(binder) { }

        public override string Type => AutomationTypeCodes.StoreGet;
        public override bool HasSideEffects => false;
        protected override string DisplayName => "Read a value";

        public override async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!TryKey(context, out var key, out var failure))
            {
                return failure!;
            }

            var store = StoreOf(context);
            if (store == null)
            {
                return StepResult.Failed("Automation services are not available.", retryable: false);
            }

            var item = await store.GetDataItemAsync(context.Run.AutomationId, key, context.UtcNow, cancellationToken);
            var fallback = Binder.Interpolate(NodeConfig.GetString(context.Node, "default"), context.Variables);
            return StepResult.Succeeded(new JsonObject
            {
                ["key"] = key,
                ["found"] = item != null,
                ["value"] = item?.Value ?? fallback
            });
        }
    }

    public class StoreDeleteActionHandler : DataStoreActionHandlerBase
    {
        public StoreDeleteActionHandler(IVariableBinder binder) : base(binder) { }

        public override string Type => AutomationTypeCodes.StoreDelete;
        public override bool HasSideEffects => true;
        protected override string DisplayName => "Forget a value";

        public override async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!TryKey(context, out var key, out var failure))
            {
                return failure!;
            }

            if (context.IsDryRun)
            {
                return StepResult.Skipped("dryRun");
            }

            var store = StoreOf(context);
            if (store == null)
            {
                return StepResult.Failed("Automation services are not available.", retryable: false);
            }

            var deleted = await store.DeleteDataItemAsync(context.Run.AutomationId, key, cancellationToken);
            return StepResult.Succeeded(new JsonObject { ["key"] = key, ["deleted"] = deleted });
        }
    }

    // "Only once": the first time a key is seen the flow goes on, every later time this branch ends quietly.
    // Typical key: {{trigger.id}} (one mail, one ticket, one order is handled one time).
    public class OnceActionHandler : DataStoreActionHandlerBase
    {
        public OnceActionHandler(IVariableBinder binder) : base(binder) { }

        public override string Type => AutomationTypeCodes.Once;
        public override bool HasSideEffects => true;
        protected override string DisplayName => "Only once";
        protected override string KeyPrefix => "once:";

        public override async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!TryKey(context, out var key, out var failure))
            {
                return failure!;
            }

            var store = StoreOf(context);
            if (store == null)
            {
                return StepResult.Failed("Automation services are not available.", retryable: false);
            }

            bool first;
            if (context.IsDryRun)
            {
                // a test only looks: it must not use up the key
                first = await store.GetDataItemAsync(context.Run.AutomationId, key, context.UtcNow, cancellationToken) == null;
            }
            else
            {
                first = await store.TryAddDataItemAsync(context.Run.AutomationId, key, "1", context.UtcNow, Expiry(context), cancellationToken);
            }

            return StepResult.Succeeded(
                new JsonObject { ["firstTime"] = first, ["key"] = key },
                port: first ? null : FilterActionHandler.StoppedPort);
        }
    }
}
