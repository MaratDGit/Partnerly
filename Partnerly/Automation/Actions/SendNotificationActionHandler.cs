using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Actions
{
    public class SendNotificationActionHandler : SideEffectActionHandlerBase
    {
        private readonly IVariableBinder _binder;

        public SendNotificationActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public override string Type => AutomationTypeCodes.SendNotification;
        public override string DisplayName => "Send notification";

        protected override async Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var notifications = context.Services.GetService(typeof(INotificationService)) as INotificationService;
            if (notifications == null)
            {
                return StepResult.Failed("Notification service is not available.");
            }

            var userRaw = _binder.Interpolate(NodeConfig.GetString(context.Node, "userId"), context.Variables);
            if (!Guid.TryParse(userRaw, out var userId))
            {
                userId = context.Run.OwnerUserId;
            }

            var result = await notifications.CreateNotificationAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = _binder.Interpolate(NodeConfig.GetString(context.Node, "notificationType") ?? "System", context.Variables),
                Message = _binder.Interpolate(NodeConfig.GetString(context.Node, "message") ?? string.Empty, context.Variables),
                Link = _binder.Interpolate(NodeConfig.GetString(context.Node, "link"), context.Variables)
            });

            return result.Success ? StepResult.Succeeded() : StepResult.Failed(string.Join(" ", result.Errors));
        }
    }
}
