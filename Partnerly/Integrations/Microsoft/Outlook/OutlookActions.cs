using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Microsoft.Outlook
{
    // Sends from the connected Outlook account. The SMTP send_email action
    // (Partnerly's own mailbox) and gmail.send stay as they are.
    public class OutlookSendAction : IntegrationActionHandlerBase
    {
        private readonly OutlookApi _outlook;

        public OutlookSendAction(OutlookApi outlook)
        {
            _outlook = outlook;
        }

        public override string Type => IntegrationTypeCodes.OutlookSend;
        public override string DisplayName => "Send Outlook email";
        public override string Group => "Outlook";
        public override string ConnectionKind => IntegrationKinds.Microsoft;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var to = OutlookText.Addresses(Read(context, binder, "to"));
            if (to.Count == 0)
            {
                return StepResult.Failed("Recipient email is required.", retryable: false);
            }

            await _outlook.SendAsync(
                connection,
                to,
                Read(context, binder, "subject"),
                Read(context, binder, "body"),
                Read(context, binder, "format") != "text",
                cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["sent"] = true,
                ["to"] = string.Join(", ", to)
            });
        }
    }

    public class OutlookFindMessagesAction : IntegrationActionHandlerBase
    {
        private readonly OutlookApi _outlook;

        public OutlookFindMessagesAction(OutlookApi outlook)
        {
            _outlook = outlook;
        }

        public override string Type => IntegrationTypeCodes.OutlookFindMessages;
        public override string DisplayName => "Find Outlook messages";
        public override string Group => "Outlook";
        public override string ConnectionKind => IntegrationKinds.Microsoft;
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var max = int.TryParse(Read(context, binder, "max"), out var value) ? value : 5;
            var messages = await _outlook.FindAsync(connection, Read(context, binder, "query"), max, cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = messages.Count,
                ["first"] = messages.Count > 0 ? messages[0].DeepClone() : null,
                ["messages"] = new JsonArray(messages.Select(item => (JsonNode?)item).ToArray())
            });
        }
    }

    public class OutlookCreateEventAction : IntegrationActionHandlerBase
    {
        private readonly OutlookApi _outlook;

        public OutlookCreateEventAction(OutlookApi outlook)
        {
            _outlook = outlook;
        }

        public override string Type => IntegrationTypeCodes.OutlookCreateEvent;
        public override string DisplayName => "Create Outlook event";
        public override string Group => "Outlook Calendar";
        public override string ConnectionKind => IntegrationKinds.Microsoft;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var subject = Read(context, binder, "subject");
            if (string.IsNullOrWhiteSpace(subject))
            {
                return StepResult.Failed("Event title is required.", retryable: false);
            }

            if (!OutlookText.TryParseLocal(Read(context, binder, "start"), out var start))
            {
                return StepResult.Failed("Start must be a date and time, e.g. 2026-11-06T10:00.", retryable: false);
            }

            var endText = Read(context, binder, "end");
            DateTime end;
            if (string.IsNullOrWhiteSpace(endText))
            {
                var minutes = int.TryParse(Read(context, binder, "durationMinutes"), out var parsed) && parsed > 0 ? parsed : 60;
                end = start.AddMinutes(minutes);
            }
            else if (!OutlookText.TryParseLocal(endText, out end) || end <= start)
            {
                return StepResult.Failed("End must be a date and time after the start.", retryable: false);
            }

            var zone = Read(context, binder, "timeZone");
            var json = await _outlook.CreateEventAsync(
                connection,
                subject,
                start,
                end,
                string.IsNullOrWhiteSpace(zone) ? "UTC" : zone,
                Read(context, binder, "body"),
                Read(context, binder, "location"),
                OutlookText.Addresses(Read(context, binder, "attendees")),
                cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["id"] = json["id"]?.ToString(),
                ["subject"] = subject,
                ["webLink"] = json["webLink"]?.ToString()
            });
        }
    }

    public class OutlookFindEventsAction : IntegrationActionHandlerBase
    {
        private readonly OutlookApi _outlook;

        public OutlookFindEventsAction(OutlookApi outlook)
        {
            _outlook = outlook;
        }

        public override string Type => IntegrationTypeCodes.OutlookFindEvents;
        public override string DisplayName => "Find Outlook events";
        public override string Group => "Outlook Calendar";
        public override string ConnectionKind => IntegrationKinds.Microsoft;
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var days = int.TryParse(Read(context, binder, "days"), out var d) ? Math.Clamp(d, 1, 90) : 7;
            var max = int.TryParse(Read(context, binder, "max"), out var m) ? m : 10;
            var now = context.Services.GetRequiredService<IAutomationClock>().UtcNow;
            var events = await _outlook.FindEventsAsync(connection, now, now.AddDays(days), max, cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = events.Count,
                ["first"] = events.Count > 0 ? events[0].DeepClone() : null,
                ["events"] = new JsonArray(events.Select(item => (JsonNode?)item).ToArray())
            });
        }
    }

    internal static class OutlookText
    {
        private static readonly System.Text.RegularExpressions.Regex AddressPattern =
            new(@"^[^@\s:,;<>]+@[^@\s:,;<>]+\.[^@\s:,;<>]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        // Comma / semicolon / line separated addresses; anything that is not a plain address is dropped.
        public static List<string> Addresses(string text) =>
            (text ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.Replace(" ", string.Empty))
                .Where(item => AddressPattern.IsMatch(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        // Wall-clock time in the event's own time zone: parsed without any zone shift.
        public static bool TryParseLocal(string text, out DateTime value) =>
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
