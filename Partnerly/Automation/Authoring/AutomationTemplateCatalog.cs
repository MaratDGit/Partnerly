using System.Text.Json;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Authoring
{
    public sealed class AutomationPreset
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Size { get; init; } = "small";
        public string NameKey { get; init; } = string.Empty;
        public string DescriptionKey { get; init; } = string.Empty;
        public string GraphJson { get; init; } = "{}";
    }

    public static class AutomationTemplateCatalog
    {
        public static IReadOnlyList<AutomationPreset> All { get; } = new[]
        {
            Make(
                "manual-flag",
                "Manual flag",
                "Run by hand and set a variable.",
                "small",
                "TplManualFlag",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("a1", "set_variable", "flag", 280, 80, @"{ ""name"": ""ready"", ""value"": ""yes"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "delay-then-flag",
                "Delay then flag",
                "Wait five seconds, then set a variable.",
                "small",
                "TplDelayThenFlag",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("d1", "delay", "pause", 280, 80, @"{ ""amount"": 5, ""unit"": ""seconds"" }"),
                    Action("a1", "set_variable", "after", 520, 80, @"{ ""name"": ""done"", ""value"": ""1"" }"),
                    Edge("e1", "t1", "d1"),
                    Edge("e2", "d1", "a1"))),
            Make(
                "daily-marker",
                "Daily marker",
                "Every day at 09:00 in the automation time zone.",
                "small",
                "TplDailyMarker",
                Graph(
                    Trigger("t1", "schedule", "morning", 40, 80, @"{ ""scheduleKind"": ""daily"", ""time"": ""09:00"" }"),
                    Action("a1", "set_variable", "tick", 280, 80, @"{ ""name"": ""ran"", ""value"": ""1"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "welcome-ping",
                "Welcome ping",
                "When a user registers, notify the agent owner.",
                "small",
                "TplWelcomePing",
                Graph(
                    Trigger("t1", "event", "signup", 40, 80, @"{ ""event"": ""UserRegistered"" }"),
                    Action("a1", "send_notification", "ping", 280, 80,
                        @"{ ""message"": ""Welcome {{trigger.firstName}} {{trigger.lastName}} ({{trigger.email}})"", ""notificationType"": ""System"", ""link"": ""/Dashboard"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "hourly-pulse",
                "Hourly pulse",
                "Every 60 minutes, store the last run time.",
                "small",
                "TplHourlyPulse",
                Graph(
                    Trigger("t1", "interval", "everyHour", 40, 80, @"{ ""minutes"": 60 }"),
                    Action("a1", "set_variable", "pulse", 280, 80,
                        @"{ ""name"": ""lastPulse"", ""value"": ""{{execution.startedAt}}"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "new-ticket-alert",
                "New ticket alert",
                "On a new support ticket: save the number, notify, then email notify@example.com.",
                "medium",
                "TplNewTicketAlert",
                Graph(
                    Trigger("t1", "entity.created", "ticketOpened", 40, 100,
                        @"{ ""entity"": ""support_ticket"", ""amount"": 0, ""unit"": ""minutes"" }"),
                    Action("a1", "set_variable", "saveNumber", 280, 100,
                        @"{ ""name"": ""ticketNo"", ""value"": ""{{trigger.after.ticketId}}"" }"),
                    Action("a2", "send_notification", "notifyOwner", 520, 100,
                        @"{ ""message"": ""New ticket {{vars.ticketNo}}: {{trigger.after.subject}}"", ""notificationType"": ""Support"", ""link"": ""/SupportTickets"" }"),
                    Action("a3", "send_email", "mailDesk", 760, 100,
                        @"{ ""to"": ""notify@example.com"", ""subject"": ""New ticket {{vars.ticketNo}}"", ""body"": ""Subject: {{trigger.after.subject}}\nStatus: {{trigger.after.status}}\nId: {{trigger.id}}"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"))),
            Make(
                "inbound-webhook",
                "Inbound webhook",
                "After publish, POST to the Connections URL. Stores a flag and notifies the owner.",
                "medium",
                "TplInboundWebhook",
                Graph(
                    Trigger("t1", "webhook", "incoming", 40, 100, @"{ ""amount"": 0, ""unit"": ""seconds"" }"),
                    Action("a1", "set_variable", "markSource", 280, 100,
                        @"{ ""name"": ""source"", ""value"": ""webhook"" }"),
                    Action("a2", "set_variable", "markId", 520, 100,
                        @"{ ""name"": ""runStamp"", ""value"": ""{{execution.runId}}"" }"),
                    Action("a3", "send_notification", "tellOwner", 760, 100,
                        @"{ ""message"": ""Inbound webhook {{vars.runStamp}}"", ""notificationType"": ""System"", ""link"": ""/Automation"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"))),
            Make(
                "weekday-briefing",
                "Weekday briefing",
                "Cron 09:00 Monday–Friday: set a flag, notify, email notify@example.com.",
                "medium",
                "TplWeekdayBriefing",
                Graph(
                    Trigger("t1", "schedule", "weekdays", 40, 100,
                        @"{ ""scheduleKind"": ""cron"", ""expression"": ""0 9 * * 1-5"" }"),
                    Action("a1", "set_variable", "markDay", 280, 100,
                        @"{ ""name"": ""briefing"", ""value"": ""weekday"" }"),
                    Action("a2", "send_notification", "ping", 520, 100,
                        @"{ ""message"": ""Weekday briefing — check Agents and tickets."", ""notificationType"": ""System"", ""link"": ""/Automation"" }"),
                    Action("a3", "send_email", "mail", 760, 100,
                        @"{ ""to"": ""notify@example.com"", ""subject"": ""Morning briefing"", ""body"": ""Good morning.\nOpen Agents to review overnight tickets.\nFlag: {{vars.briefing}}"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"))),
            Make(
                "new-user-onboarding",
                "New user onboarding",
                "UserRegistered: wait 2 minutes, welcome email, notify, then branch on email and open a ticket.",
                "large",
                "TplNewUserOnboarding",
                Graph(
                    Trigger("t1", "event", "signup", 40, 160, @"{ ""event"": ""UserRegistered"" }"),
                    Action("d1", "delay", "pause", 260, 160, @"{ ""amount"": 2, ""unit"": ""minutes"" }"),
                    Action("m1", "send_email", "welcomeMail", 480, 160,
                        @"{ ""to"": ""{{trigger.email}}"", ""subject"": ""Welcome {{trigger.firstName}}"", ""body"": ""Hi {{trigger.firstName}} {{trigger.lastName}},\n\nYour Partnerly account is ready.\nEmail: {{trigger.email}}"" }"),
                    Action("n1", "send_notification", "welcomePing", 700, 160,
                        @"{ ""message"": ""Welcome {{trigger.firstName}}"", ""userId"": ""{{trigger.id}}"", ""notificationType"": ""System"", ""link"": ""/Dashboard"" }"),
                    Action("s1", "set_variable", "markStage", 920, 160,
                        @"{ ""name"": ""stage"", ""value"": ""welcomed"" }"),
                    Action("b1", "branch", "hasEmail", 1140, 160,
                        @"{ ""condition"": { ""op"": ""is_not_empty"", ""left"": ""{{trigger.email}}"" } }"),
                    Action("c1", "create_record", "openTicket", 1380, 60,
                        @"{ ""entity"": ""support_ticket"", ""fields"": { ""subject"": ""Onboarding {{trigger.firstName}}"", ""message"": ""New user {{trigger.email}} ({{trigger.id}})"", ""status"": ""New"", ""userId"": ""{{trigger.id}}"", ""note"": ""Created by onboarding agent"" } }"),
                    Action("s2", "set_variable", "markDone", 1620, 60,
                        @"{ ""name"": ""onboarded"", ""value"": ""1"" }"),
                    Action("n2", "send_notification", "missingEmail", 1380, 260,
                        @"{ ""message"": ""User {{trigger.id}} registered without email"", ""notificationType"": ""System"" }"),
                    Edge("e1", "t1", "d1"),
                    Edge("e2", "d1", "m1"),
                    Edge("e3", "m1", "n1"),
                    Edge("e4", "n1", "s1"),
                    Edge("e5", "s1", "b1"),
                    Edge("e6", "b1", "c1", "true"),
                    Edge("e7", "c1", "s2"),
                    Edge("e8", "b1", "n2", "false"))),
            Make(
                "ticket-triage",
                "Ticket triage desk",
                "New support ticket: variables, notify, branch on New, email, create notification, find, HTTP and outbound webhook.",
                "large",
                "TplTicketTriage",
                Graph(
                    Trigger("t1", "entity.created", "ticketOpened", 40, 200,
                        @"{ ""entity"": ""support_ticket"", ""amount"": 0, ""unit"": ""minutes"" }"),
                    Action("s1", "set_variable", "saveNumber", 260, 200,
                        @"{ ""name"": ""ticketNo"", ""value"": ""{{trigger.after.ticketId}}"" }"),
                    Action("s2", "set_variable", "saveSubject", 480, 200,
                        @"{ ""name"": ""subject"", ""value"": ""{{trigger.after.subject}}"" }"),
                    Action("n1", "send_notification", "deskPing", 700, 200,
                        @"{ ""message"": ""Ticket {{vars.ticketNo}}: {{vars.subject}}"", ""notificationType"": ""Support"", ""link"": ""/SupportTickets"", ""userId"": ""{{trigger.after.assignedTo}}"" }"),
                    Action("b1", "branch", "isNew", 920, 200,
                        @"{ ""condition"": { ""op"": ""equals"", ""left"": ""{{trigger.after.status}}"", ""right"": ""New"" } }"),
                    Action("d1", "delay", "hold", 1140, 80, @"{ ""amount"": 5, ""unit"": ""minutes"" }"),
                    Action("m1", "send_email", "followUp", 1360, 80,
                        @"{ ""to"": ""notify@example.com"", ""subject"": ""Follow-up {{vars.ticketNo}}"", ""body"": ""Still New: {{vars.subject}}\nTicket id {{trigger.id}}"" }"),
                    Action("c1", "create_record", "logNote", 1580, 80,
                        @"{ ""entity"": ""notification"", ""fields"": { ""type"": ""Support"", ""message"": ""Follow-up sent for {{vars.ticketNo}}"", ""userId"": ""{{trigger.after.userId}}"", ""link"": ""/SupportTickets"" } }"),
                    Action("f1", "find_record", "lookup", 1140, 320,
                        @"{ ""entity"": ""support_ticket"", ""filters"": { ""id"": ""{{trigger.id}}"" } }"),
                    Action("h1", "http_request", "pingSite", 1360, 320,
                        @"{ ""method"": ""GET"", ""url"": ""https://example.com"" }"),
                    Action("w1", "webhook", "pushHook", 1580, 320,
                        @"{ ""url"": ""https://example.com/hooks/partnerly"", ""body"": ""{\""ticket\"":\""{{vars.ticketNo}}\"",\""subject\"":\""{{vars.subject}}\"",\""id\"":\""{{trigger.id}}\""}"", ""secret"": """" }"),
                    Action("u1", "wait_until", "hasSubject", 1800, 320,
                        @"{ ""condition"": { ""op"": ""is_not_empty"", ""left"": ""{{vars.subject}}"" } }"),
                    Edge("e1", "t1", "s1"),
                    Edge("e2", "s1", "s2"),
                    Edge("e3", "s2", "n1"),
                    Edge("e4", "n1", "b1"),
                    Edge("e5", "b1", "d1", "true"),
                    Edge("e6", "d1", "m1"),
                    Edge("e7", "m1", "c1"),
                    Edge("e8", "b1", "f1", "false"),
                    Edge("e9", "f1", "h1"),
                    Edge("e10", "h1", "w1"),
                    Edge("e11", "w1", "u1")))
        };

        public static AutomationPreset? Get(string? id) =>
            All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

        private static AutomationPreset Make(
            string id,
            string name,
            string description,
            string size,
            string nameKey,
            WorkflowGraph graph)
        {
            return new AutomationPreset
            {
                Id = id,
                Name = name,
                Description = description,
                Size = size,
                NameKey = nameKey,
                DescriptionKey = nameKey + "Body",
                GraphJson = graph.ToJson()
            };
        }

        private static WorkflowGraph Graph(params object[] parts)
        {
            var graph = new WorkflowGraph { SchemaVersion = 1 };
            foreach (var part in parts)
            {
                if (part is WorkflowNode node)
                {
                    graph.Nodes.Add(node);
                }
                else if (part is WorkflowEdge edge)
                {
                    graph.Edges.Add(edge);
                }
            }

            return graph;
        }

        private static WorkflowNode Trigger(string id, string type, string name, double x, double y, string config = "{}") =>
            Node(id, "trigger", type, name, x, y, config);

        private static WorkflowNode Action(string id, string type, string name, double x, double y, string config = "{}") =>
            Node(id, "action", type, name, x, y, config);

        private static WorkflowNode Node(string id, string kind, string type, string name, double x, double y, string config)
        {
            return new WorkflowNode
            {
                Id = id,
                Kind = kind,
                Type = type,
                Name = name,
                X = x,
                Y = y,
                Config = JsonSerializer.Deserialize<JsonElement>(config)
            };
        }

        private static WorkflowEdge Edge(string id, string from, string to, string? port = null) =>
            new() { Id = id, From = from, To = to, Port = port };
    }
}
