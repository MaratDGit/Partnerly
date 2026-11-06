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

        // partnerly / telegram / google / whatsapp — filter chip and card color on the hub.
        public string Category { get; init; } = "partnerly";

        // Service names used by the graph (Telegram, Google Sheets…), shown as chips.
        public IReadOnlyList<string> Services { get; init; } = Array.Empty<string>();

        public int StepCount { get; init; }
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
                    Edge("e11", "w1", "u1"))),

            // ---------- Integrations (Partnerly/Integrations). connectionId is chosen in the builder. ----------
            Make(
                "telegram-ticket-alert",
                "Ticket to Telegram",
                "When a support ticket opens, post it to your Telegram chat.",
                "small",
                "TplTelegramTicketAlert",
                Graph(
                    Trigger("t1", "event", "ticketOpened", 40, 80, @"{ ""event"": ""SupportTicketCreated"" }"),
                    Action("a1", "telegram.send_message", "tellTeam", 300, 80,
                        @"{ ""text"": ""🆕 Ticket {{trigger.ticketId}}\n{{trigger.subject}}"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "telegram-bot-inbox",
                "Telegram bot inbox",
                "Message to the bot: notify you and reply automatically.",
                "medium",
                "TplTelegramBotInbox",
                Graph(
                    Trigger("t1", "telegram.message_received", "botMessage", 40, 80, "{}"),
                    Action("a1", "send_notification", "notifyMe", 300, 80,
                        @"{ ""message"": ""Telegram {{trigger.fromName}}: {{trigger.text}}"", ""notificationType"": ""System"" }"),
                    Action("a2", "telegram.send_message", "autoReply", 560, 80,
                        @"{ ""chatId"": ""{{trigger.chatId}}"", ""text"": ""Thanks, {{trigger.fromName}}! We got your message and will answer soon."", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "sheets-signup-log",
                "Sign-ups to Google Sheets",
                "Every new user becomes a row: name, email and time.",
                "small",
                "TplSheetsSignupLog",
                Graph(
                    Trigger("t1", "event", "signup", 40, 80, @"{ ""event"": ""UserRegistered"" }"),
                    Action("a1", "sheets.append_row", "logRow", 300, 80,
                        @"{ ""sheet"": ""Sheet1"", ""values"": ""{{trigger.firstName}}\n{{trigger.lastName}}\n{{trigger.email}}\n{{execution.startedAt}}"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "sheets-lead-welcome",
                "Sheet lead to Gmail welcome",
                "A new row in a leads sheet sends a welcome email from your Gmail.",
                "medium",
                "TplSheetsLeadWelcome",
                Graph(
                    Trigger("t1", "sheets.row_added", "newLead", 40, 80, @"{ ""sheet"": ""Leads"" }"),
                    Action("a1", "gmail.send", "welcomeMail", 300, 80,
                        @"{ ""to"": ""{{trigger.fields.Email}}"", ""subject"": ""Welcome, {{trigger.fields.Name}}"", ""body"": ""<p>Hi {{trigger.fields.Name}},</p><p>Thanks for your interest. We will contact you soon.</p>"", ""format"": ""html"" }"),
                    Action("a2", "send_notification", "notifyMe", 560, 80,
                        @"{ ""message"": ""Welcome email sent to {{trigger.fields.Email}}"", ""notificationType"": ""System"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "gmail-to-telegram",
                "Gmail to Telegram",
                "Each new inbox email is forwarded as a short Telegram message.",
                "small",
                "TplGmailToTelegram",
                Graph(
                    Trigger("t1", "gmail.message_received", "newMail", 40, 80, "{}"),
                    Action("a1", "telegram.send_message", "forward", 300, 80,
                        @"{ ""text"": ""📧 {{trigger.from}}\n{{trigger.subject}}\n{{trigger.snippet}}"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "outlook-to-telegram",
                "Outlook to Telegram",
                "Each new inbox email in Outlook is forwarded as a short Telegram message.",
                "small",
                "TplOutlookToTelegram",
                Graph(
                    Trigger("t1", "outlook.message_received", "newMail", 40, 80, "{}"),
                    Action("a1", "telegram.send_message", "forward", 300, 80,
                        @"{ ""text"": ""📧 {{trigger.from}}\n{{trigger.subject}}\n{{trigger.snippet}}"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "signup-outlook-welcome",
                "Welcome email from Outlook",
                "A new user receives a welcome email sent from your Outlook account.",
                "small",
                "TplSignupOutlookWelcome",
                Graph(
                    Trigger("t1", "event", "signup", 40, 80, @"{ ""event"": ""UserRegistered"" }"),
                    Action("a1", "outlook.send", "welcomeMail", 300, 80,
                        @"{ ""to"": ""{{trigger.email}}"", ""subject"": ""Welcome, {{trigger.firstName}}"", ""body"": ""<p>Hi {{trigger.firstName}},</p><p>Welcome aboard!</p>"", ""format"": ""html"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "drive-file-alert",
                "New Drive file alert",
                "A new file in a Drive folder sends you a notification with its link.",
                "small",
                "TplDriveFileAlert",
                Graph(
                    Trigger("t1", "drive.file_added", "newFile", 40, 80, "{}"),
                    Action("a1", "send_notification", "notifyMe", 300, 80,
                        @"{ ""message"": ""New file in Drive: {{trigger.name}}"", ""notificationType"": ""System"", ""link"": ""{{trigger.webViewLink}}"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "sheets-connection-test",
                "Sheets connection test",
                "Run by hand: adds a test row with the run time to your sheet.",
                "small",
                "TplSheetsConnectionTest",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("a1", "sheets.append_row", "testRow", 300, 80,
                        @"{ ""sheet"": ""Sheet1"", ""values"": ""Partnerly test\n{{execution.startedAt}}\n{{execution.runId}}"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "sheets-row-to-telegram",
                "New sheet row to Telegram",
                "Each new row in a sheet is announced in Telegram.",
                "small",
                "TplSheetsRowToTelegram",
                Graph(
                    Trigger("t1", "sheets.row_added", "newRow", 40, 80, @"{ ""sheet"": ""Sheet1"" }"),
                    Action("a1", "telegram.send_message", "announce", 300, 80,
                        @"{ ""text"": ""📊 New row {{trigger.rowNumber}} in {{trigger.sheet}}"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"))),
            Make(
                "sheets-find-and-report",
                "Find rows and report",
                "Search a column for a value, then notify whether anything was found.",
                "medium",
                "TplSheetsFindAndReport",
                Graph(
                    Trigger("t1", "manual", "start", 40, 160),
                    Action("f1", "sheets.find_rows", "lookup", 280, 160,
                        @"{ ""sheet"": ""Sheet1"", ""column"": ""A"", ""value"": ""test"" }"),
                    Action("b1", "branch", "anyFound", 520, 160,
                        @"{ ""condition"": { ""op"": ""greater_than"", ""left"": ""{{steps.lookup.result.count}}"", ""right"": ""0"" } }"),
                    Action("n1", "send_notification", "found", 760, 60,
                        @"{ ""message"": ""Found {{steps.lookup.result.count}} row(s), first at row {{steps.lookup.result.first.rowNumber}}"", ""notificationType"": ""System"" }"),
                    Action("n2", "send_notification", "notFound", 760, 260,
                        @"{ ""message"": ""No matching rows found"", ""notificationType"": ""System"" }"),
                    Edge("e1", "t1", "f1"),
                    Edge("e2", "f1", "b1"),
                    Edge("e3", "b1", "n1", "true"),
                    Edge("e4", "b1", "n2", "false"))),
            Make(
                "drive-folder-and-log",
                "Create Drive folder and log it",
                "Run by hand: creates a Drive folder, logs its link to Sheets and notifies you.",
                "medium",
                "TplDriveFolderAndLog",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("a1", "drive.create_folder", "makeFolder", 280, 80,
                        @"{ ""name"": ""Partnerly test {{execution.startedAt}}"" }"),
                    Action("a2", "sheets.append_row", "logFolder", 520, 80,
                        @"{ ""sheet"": ""Sheet1"", ""values"": ""{{steps.makeFolder.result.name}}\n{{steps.makeFolder.result.webViewLink}}\n{{execution.startedAt}}"" }"),
                    Action("a3", "send_notification", "notifyMe", 760, 80,
                        @"{ ""message"": ""Folder created: {{steps.makeFolder.result.name}}"", ""notificationType"": ""System"", ""link"": ""{{steps.makeFolder.result.webViewLink}}"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"))),
            Make(
                "drive-file-to-sheet",
                "Drive file to Sheets and Telegram",
                "A new Drive file is logged to a sheet and announced in Telegram.",
                "medium",
                "TplDriveFileToSheet",
                Graph(
                    Trigger("t1", "drive.file_added", "newFile", 40, 80, "{}"),
                    Action("a1", "sheets.append_row", "logFile", 300, 80,
                        @"{ ""sheet"": ""Sheet1"", ""values"": ""{{trigger.name}}\n{{trigger.mimeType}}\n{{trigger.webViewLink}}\n{{trigger.createdTime}}"" }"),
                    Action("a2", "telegram.send_message", "announce", 560, 80,
                        @"{ ""text"": ""📁 {{trigger.name}}\n{{trigger.webViewLink}}"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "drive-find-report",
                "Find Drive files",
                "Run by hand: looks for files by name and notifies how many were found.",
                "small",
                "TplDriveFindReport",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("f1", "drive.find_files", "lookup", 280, 80,
                        @"{ ""nameContains"": ""report"", ""max"": 10 }"),
                    Action("n1", "send_notification", "result", 520, 80,
                        @"{ ""message"": ""Found {{steps.lookup.result.count}} file(s). First: {{steps.lookup.result.first.name}}"", ""notificationType"": ""System"", ""link"": ""{{steps.lookup.result.first.webViewLink}}"" }"),
                    Edge("e1", "t1", "f1"),
                    Edge("e2", "f1", "n1"))),
            Make(
                "whatsapp-auto-reply",
                "WhatsApp auto-reply and log",
                "Answer incoming WhatsApp messages and log them to Google Sheets.",
                "medium",
                "TplWhatsAppAutoReply",
                Graph(
                    Trigger("t1", "whatsapp.message_received", "incoming", 40, 80, "{}"),
                    Action("a1", "whatsapp.send_text", "reply", 300, 80,
                        @"{ ""to"": ""{{trigger.from}}"", ""text"": ""Hi {{trigger.name}}! We received your message and will reply soon."" }"),
                    Action("a2", "sheets.append_row", "logChat", 560, 80,
                        @"{ ""sheet"": ""WhatsApp"", ""values"": ""{{trigger.from}}\n{{trigger.name}}\n{{trigger.text}}\n{{execution.startedAt}}"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "omnichannel-ticket",
                "Ticket to every channel",
                "New ticket: Telegram team chat, Sheets log and Gmail to the lead; urgent ones also go to WhatsApp.",
                "large",
                "TplOmnichannelTicket",
                Graph(
                    Trigger("t1", "event", "ticketOpened", 40, 160, @"{ ""event"": ""SupportTicketCreated"" }"),
                    Action("a1", "telegram.send_message", "teamChat", 280, 160,
                        @"{ ""text"": ""🆕 {{trigger.ticketId}}: {{trigger.subject}}"", ""parseMode"": ""none"" }"),
                    Action("a2", "sheets.append_row", "logTicket", 520, 160,
                        @"{ ""sheet"": ""Tickets"", ""values"": ""{{trigger.ticketId}}\n{{trigger.subject}}\n{{trigger.status}}\n{{execution.startedAt}}"" }"),
                    Action("a3", "gmail.send", "mailLead", 760, 160,
                        @"{ ""to"": ""lead@example.com"", ""subject"": ""New ticket {{trigger.ticketId}}"", ""body"": ""<p>{{trigger.subject}}</p><p>Status: {{trigger.status}}</p>"", ""format"": ""html"" }"),
                    Action("b1", "branch", "isUrgent", 1000, 160,
                        @"{ ""condition"": { ""op"": ""contains"", ""left"": ""{{trigger.subject}}"", ""right"": ""urgent"" } }"),
                    Action("w1", "whatsapp.send_template", "callManager", 1240, 60,
                        @"{ ""to"": ""37400000000"", ""template"": ""urgent_ticket"", ""language"": ""en_US"", ""parameters"": ""{{trigger.ticketId}}\n{{trigger.subject}}"" }"),
                    Action("s1", "set_variable", "normal", 1240, 260, @"{ ""name"": ""priority"", ""value"": ""normal"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"),
                    Edge("e4", "a3", "b1"),
                    Edge("e5", "b1", "w1", "true"),
                    Edge("e6", "b1", "s1", "false"))),
            Make(
                "pc-new-file-telegram",
                "New PC file to Telegram",
                "When a PDF or Word file appears in a watched folder, send it to Telegram.",
                "medium",
                "TplPcNewFile",
                Graph(
                    Trigger("t1", "computer.file_created", "newFile", 40, 80, @"{ ""folder"": """", ""pattern"": ""*.pdf;*.docx"" }"),
                    Action("a1", "computer.get_file", "fetch", 300, 80, @"{ ""path"": ""{{trigger.path}}"" }"),
                    Action("a2", "telegram.send_document", "sendFile", 560, 80,
                        @"{ ""attachmentId"": ""{{steps.fetch.result.attachmentId}}"", ""caption"": ""📄 {{trigger.fileName}} ({{trigger.machine}})"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "pc-screenshot-telegram",
                "PC screenshot to Telegram",
                "Run by hand: takes a screenshot of the PC and sends it to Telegram.",
                "small",
                "TplPcScreenshot",
                Graph(
                    Trigger("t1", "manual", "start", 40, 80),
                    Action("a1", "computer.screenshot", "shot", 280, 80, @"{ ""screen"": ""main"" }"),
                    Action("a2", "telegram.send_document", "sendShot", 520, 80,
                        @"{ ""attachmentId"": ""{{steps.shot.result.attachmentId}}"", ""caption"": ""🖥 Screenshot {{execution.startedAt}}"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "pc-disk-low-alert",
                "Low disk space alert",
                "When a disk is almost full: message on the PC, a Partnerly notification and a Telegram message.",
                "medium",
                "TplPcDiskLow",
                Graph(
                    Trigger("t1", "computer.disk_low", "diskLow", 40, 100, "{}"),
                    Action("a1", "computer.notify", "warnPc", 300, 100,
                        @"{ ""title"": ""Disk space"", ""text"": ""Drive {{trigger.drive}} has only {{trigger.freeGb}} GB left."" }"),
                    Action("a2", "send_notification", "notifyMe", 560, 100,
                        @"{ ""message"": ""{{trigger.machine}}: drive {{trigger.drive}} has {{trigger.freeGb}} GB free ({{trigger.freePercent}}%)"", ""notificationType"": ""System"" }"),
                    Action("a3", "telegram.send_message", "tellTelegram", 820, 100,
                        @"{ ""text"": ""💾 {{trigger.machine}}: drive {{trigger.drive}} has {{trigger.freeGb}} GB free"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"),
                    Edge("e3", "a2", "a3"))),
            Make(
                "pc-offline-alert",
                "PC went offline",
                "When the PC loses its connection to Partnerly, notify you and tell Telegram.",
                "small",
                "TplPcOffline",
                Graph(
                    Trigger("t1", "computer.status_changed", "pcStatus", 40, 80, @"{ ""state"": ""offline"" }"),
                    Action("a1", "send_notification", "notifyMe", 300, 80,
                        @"{ ""message"": ""{{trigger.machine}} is offline"", ""notificationType"": ""System"" }"),
                    Action("a2", "telegram.send_message", "tellTelegram", 560, 80,
                        @"{ ""text"": ""⚠️ {{trigger.machine}} is offline"", ""parseMode"": ""none"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2"))),
            Make(
                "pc-morning-greeting",
                "Good morning on the PC",
                "Every day at 09:00 the PC shows a notification and says a greeting aloud.",
                "small",
                "TplPcMorning",
                Graph(
                    Trigger("t1", "schedule", "morning", 40, 80, @"{ ""scheduleKind"": ""daily"", ""time"": ""09:00"" }"),
                    Action("a1", "computer.notify", "hello", 280, 80,
                        @"{ ""title"": ""Good morning"", ""text"": ""A new day has started."" }"),
                    Action("a2", "computer.speak", "sayHello", 520, 80, @"{ ""text"": ""Good morning!"" }"),
                    Edge("e1", "t1", "a1"),
                    Edge("e2", "a1", "a2")))
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
            var services = graph.Nodes
                .Select(node => ServiceOf(node.Type))
                .Where(service => service != null)
                .Select(service => service!)
                .Distinct()
                .ToList();
            return new AutomationPreset
            {
                Id = id,
                Name = name,
                Description = description,
                Size = size,
                NameKey = nameKey,
                DescriptionKey = nameKey + "Body",
                GraphJson = graph.ToJson(),
                Services = services,
                Category = services.Count == 0
                    ? "partnerly"
                    : services.Contains("Computer") ? "computer" : CategoryOf(services[0]),
                StepCount = graph.Nodes.Count(node => node.Kind == "action")
            };
        }

        // Integration node types are "<service>.<action>" (see Partnerly/Integrations).
        private static string? ServiceOf(string type)
        {
            var dot = type.IndexOf('.');
            return dot <= 0 ? null : type[..dot] switch
            {
                "telegram" => "Telegram",
                "sheets" => "Google Sheets",
                "gmail" => "Gmail",
                "drive" => "Google Drive",
                "outlook" => "Outlook",
                "whatsapp" => "WhatsApp",
                "computer" => "Computer",
                _ => null
            };
        }

        private static string CategoryOf(string service) => service switch
        {
            "Telegram" => "telegram",
            "WhatsApp" => "whatsapp",
            "Outlook" => "microsoft",
            _ => "google"
        };

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
