// Property forms for integration nodes in the agent builder.
// builder.js calls this with its t(key, fallback) translator.
(function () {
    "use strict";

    window.partnerlyIntegrationNodeForms = function (t) {
        function connection(kind) {
            return { key: "connectionId", type: "connection", kind: kind, label: t("intConnection", "Connection"), help: t("intConnectionHelp", "") };
        }

        function text(key, labelKey, fallback, helpKey, plain) {
            return { key: key, type: "text", plain: !!plain, label: t(labelKey, fallback), help: helpKey ? t(helpKey, "") : "" };
        }

        function area(key, labelKey, fallback, helpKey) {
            return { key: key, type: "textarea", label: t(labelKey, fallback), help: helpKey ? t(helpKey, "") : "" };
        }

        function number(key, labelKey, fallback) {
            return { key: key, type: "number", min: 1, label: t(labelKey, fallback) };
        }

        function note(helpKey) {
            return { type: "note", help: t(helpKey, "") };
        }

        var sheetFields = [
            text("spreadsheetId", "intSpreadsheet", "Spreadsheet", "intSpreadsheetHelp"),
            text("sheet", "intSheet", "Sheet (tab)", "intSheetHelp", true)
        ];

        return {
            // ---------- Telegram ----------
            "telegram.send_message": [
                connection("telegram"),
                text("chatId", "intChatId", "Chat id", "intChatIdHelp"),
                area("text", "intMessageText", "Message", "intVariablesHelp"),
                {
                    key: "parseMode",
                    type: "select",
                    label: t("intParseMode", "Formatting"),
                    options: [
                        { value: "none", label: t("intPlainText", "Plain text") },
                        { value: "HTML", label: "HTML" },
                        { value: "MarkdownV2", label: "MarkdownV2" }
                    ]
                }
            ],
            "telegram.send_document": [
                connection("telegram"),
                text("chatId", "intChatId", "Chat id", "intChatIdHelp"),
                text("attachmentId", "intAttachment", "Attachment id", "intAttachmentHelp"),
                text("caption", "intCaption", "Caption")
            ],
            "telegram.message_received": [
                connection("telegram"),
                text("chatId", "intChatFilter", "Only this chat id"),
                text("textContains", "intTextContains", "Text contains"),
                note("intTelegramTriggerHelp")
            ],

            // ---------- Google Sheets ----------
            "sheets.append_row": [connection("google")].concat(sheetFields, [
                area("values", "intRowValues", "Row values", "intRowValuesHelp")
            ]),
            "sheets.update_row": [connection("google")].concat(sheetFields, [
                text("rowNumber", "intRowNumber", "Row number", "intVariablesHelp"),
                area("values", "intRowValues", "Row values", "intRowValuesHelp")
            ]),
            "sheets.find_rows": [connection("google")].concat(sheetFields, [
                text("column", "intColumn", "Column", "intColumnHelp", true),
                text("value", "intSearchValue", "Value"),
                note("intFindRowsHelp")
            ]),
            "sheets.row_added": [connection("google")].concat(sheetFields, [
                note("intSheetsTriggerHelp")
            ]),

            // ---------- Gmail ----------
            "gmail.send": [
                connection("google"),
                text("to", "emailTo", "To"),
                text("subject", "emailSubject", "Subject"),
                area("body", "emailBody", "Body", "intVariablesHelp"),
                {
                    key: "format",
                    type: "select",
                    label: t("intFormat", "Format"),
                    options: [
                        { value: "html", label: "HTML" },
                        { value: "text", label: t("intPlainText", "Plain text") }
                    ]
                }
            ],
            "gmail.find_messages": [
                connection("google"),
                text("query", "intGmailQuery", "Search", "intGmailQueryHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "gmail.message_received": [
                connection("google"),
                text("fromContains", "intFromContains", "From contains"),
                text("subjectContains", "intSubjectContains", "Subject contains"),
                note("intGmailTriggerHelp")
            ],

            // ---------- Outlook (mail + calendar) ----------
            "outlook.send": [
                connection("microsoft"),
                text("to", "emailTo", "To", "intOutlookToHelp"),
                text("subject", "emailSubject", "Subject"),
                area("body", "emailBody", "Body", "intVariablesHelp"),
                {
                    key: "format",
                    type: "select",
                    label: t("intFormat", "Format"),
                    options: [
                        { value: "html", label: "HTML" },
                        { value: "text", label: t("intPlainText", "Plain text") }
                    ]
                }
            ],
            "outlook.find_messages": [
                connection("microsoft"),
                text("query", "intGmailQuery", "Search", "intOutlookQueryHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "outlook.message_received": [
                connection("microsoft"),
                text("fromContains", "intFromContains", "From contains"),
                text("subjectContains", "intSubjectContains", "Subject contains"),
                note("intOutlookTriggerHelp")
            ],
            "outlook.create_event": [
                connection("microsoft"),
                text("subject", "intEventTitle", "Title"),
                text("start", "intEventStart", "Start", "intEventStartHelp"),
                text("end", "intEventEnd", "End (optional)"),
                number("durationMinutes", "intEventDuration", "Duration, minutes"),
                text("timeZone", "intTimeZone", "Time zone", "intTimeZoneHelp", true),
                text("attendees", "intAttendees", "Attendees", "intAttendeesHelp"),
                text("location", "intLocation", "Location"),
                area("body", "emailBody", "Description", "intVariablesHelp")
            ],
            "outlook.event_added": [
                connection("microsoft"),
                text("subjectContains", "intSubjectContains", "Subject contains"),
                note("intOutlookEventTriggerHelp")
            ],
            "outlook.find_events": [
                connection("microsoft"),
                number("days", "intEventDays", "Days ahead"),
                number("max", "intMaxResults", "Max results")
            ],

            // ---------- Google Drive ----------
            "drive.upload_file": [
                connection("google"),
                text("attachmentId", "intAttachment", "Attachment id", "intAttachmentHelp"),
                text("fileName", "intFileName", "File name"),
                text("folderId", "intFolderId", "Folder id", "intFolderIdHelp")
            ],
            "drive.create_folder": [
                connection("google"),
                text("name", "intFolderName", "Folder name"),
                text("parentId", "intFolderId", "Parent folder id", "intFolderIdHelp")
            ],
            "drive.find_files": [
                connection("google"),
                text("nameContains", "intNameContains", "Name contains"),
                text("folderId", "intFolderId", "Folder id", "intFolderIdHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "drive.file_added": [
                connection("google"),
                text("folderId", "intFolderId", "Folder id", "intFolderIdHelp"),
                text("nameContains", "intNameContains", "Name contains"),
                note("intDriveTriggerHelp")
            ],

            // ---------- WhatsApp ----------
            "whatsapp.send_text": [
                connection("whatsapp"),
                text("to", "intPhone", "Phone", "intPhoneHelp"),
                area("text", "intMessageText", "Message", "intWhatsAppWindowHelp")
            ],
            "whatsapp.send_template": [
                connection("whatsapp"),
                text("to", "intPhone", "Phone", "intPhoneHelp"),
                text("template", "intTemplateName", "Template name", "", true),
                text("language", "intLanguage", "Language code", "", true),
                area("parameters", "intTemplateParams", "Parameters", "intRowValuesHelp")
            ],
            "whatsapp.message_received": [
                connection("whatsapp"),
                text("textContains", "intTextContains", "Text contains"),
                note("intWhatsAppTriggerHelp")
            ]
        };
    };
})();
