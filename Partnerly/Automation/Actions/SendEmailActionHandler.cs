using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Automation.Actions
{
    public class SendEmailActionHandler : SideEffectActionHandlerBase
    {
        private readonly IVariableBinder _binder;

        public SendEmailActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public override string Type => AutomationTypeCodes.SendEmail;
        public override string DisplayName => "Send email";

        protected override async Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var email = context.Services.GetService(typeof(IEmailSender)) as IEmailSender;
            if (email == null)
            {
                return StepResult.Failed("Email sender is not available.");
            }

            var to = _binder.Interpolate(NodeConfig.GetString(context.Node, "to"), context.Variables);
            if (string.IsNullOrWhiteSpace(to))
            {
                return StepResult.Failed("Email recipient is required.");
            }

            var template = _binder.Interpolate(NodeConfig.GetString(context.Node, "template"), context.Variables);
            if (!string.IsNullOrWhiteSpace(template))
            {
                await email.SendEmailWithTemplateAsync(template, to, context.Variables);
                return StepResult.Succeeded();
            }

            var subject = _binder.Interpolate(NodeConfig.GetString(context.Node, "subject") ?? "Partnerly", context.Variables);
            var body = _binder.Interpolate(NodeConfig.GetString(context.Node, "body") ?? string.Empty, context.Variables);
            await email.SendEmailWithoutTemplateAsync(to, subject, body);
            return StepResult.Succeeded();
        }
    }
}
