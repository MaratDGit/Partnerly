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

        // A dropdown of the connection's chats (and variables) that still accepts a typed value.
        function chat(key, labelKey, fallback, helpKey) {
            return { key: key, type: "text", source: "telegramChats", label: t(labelKey, fallback), help: helpKey ? t(helpKey, "") : "" };
        }

        function area(key, labelKey, fallback, helpKey) {
            return { key: key, type: "textarea", label: t(labelKey, fallback), help: helpKey ? t(helpKey, "") : "" };
        }

        function number(key, labelKey, fallback) {
            return { key: key, type: "number", min: 1, label: t(labelKey, fallback) };
        }

        // A Yes / No choice stored as "true" / "false".
        function yesNo(key, labelKey, fallback) {
            return {
                key: key,
                type: "select",
                label: t(labelKey, fallback),
                options: [
                    { value: "false", label: t("intNo", "No") },
                    { value: "true", label: t("intYes", "Yes") }
                ]
            };
        }

        function note(helpKey) {
            return { type: "note", help: t(helpKey, "") };
        }

        // A dropdown whose options are [value, labelKey, fallback]; a null key shows the fallback as it is.
        function choice(key, labelKey, fallback, options) {
            return {
                key: key,
                type: "select",
                label: t(labelKey, fallback),
                options: options.map(function (item) { return { value: item[0], label: item[1] ? t(item[1], item[2]) : item[2] }; })
            };
        }

        // GitHub cards: the repository ("owner/name" or a link) and the number of an issue / pull request.
        function repo() {
            return text("repo", "intGhRepo", "Repository", "intGhRepoHelp", true);
        }

        function numberField() {
            return text("number", "intGhNumber", "Number", "intGhNumberHelp", true);
        }

        var sheetFields = [
            text("spreadsheetId", "intSpreadsheet", "Spreadsheet", "intSpreadsheetHelp"),
            text("sheet", "intSheet", "Sheet (tab)", "intSheetHelp", true)
        ];

        var forms = {
            // ---------- Telegram ----------
            "telegram.send_message": [
                connection("telegram"),
                chat("chatId", "intChatId", "Chat id", "intChatIdHelp"),
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
                chat("chatId", "intChatId", "Chat id", "intChatIdHelp"),
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

            "sheets.get_row": [connection("google")].concat(sheetFields, [text("rowNumber", "intRowNumber", "Row number", "intVariablesHelp")]),
            "sheets.read_range": [connection("google")].concat(sheetFields, [
                text("range", "intCellRange", "Range", "intCellRangeHelp", true)
            ]),
            "sheets.update_cell": [connection("google")].concat(sheetFields, [
                text("cell", "intCell", "Cell", "intCellHelp", true),
                text("value", "intSearchValue", "Value")
            ]),
            "sheets.clear_range": [connection("google")].concat(sheetFields, [
                text("range", "intCellRange", "Range", "intCellRangeHelp", true)
            ]),
            "sheets.delete_row": [connection("google")].concat(sheetFields, [text("rowNumber", "intRowNumber", "Row number", "intVariablesHelp")]),
            "sheets.create_spreadsheet": [
                connection("google"),
                text("title", "intSpreadsheetTitle", "Spreadsheet title"),
                text("sheet", "intFirstSheet", "First tab name (optional)", "", true)
            ],
            "sheets.add_sheet": [
                connection("google"),
                text("spreadsheetId", "intSpreadsheet", "Spreadsheet", "intSpreadsheetHelp"),
                text("sheet", "intNewSheetName", "New tab name", "", true)
            ],

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

            "gmail.reply": [
                connection("google"),
                text("messageId", "intMessageId", "Message id", "intMessageIdHelp"),
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
            "gmail.get_message": [
                connection("google"),
                text("messageId", "intMessageId", "Message id", "intMessageIdHelp")
            ],
            "gmail.create_draft": [
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
            "gmail.mark_read": [
                connection("google"),
                text("messageId", "intMessageId", "Message id", "intMessageIdHelp"),
                {
                    key: "state",
                    type: "select",
                    label: t("intMarkState", "Mark as"),
                    options: [
                        { value: "read", label: t("intStateRead", "Read") },
                        { value: "unread", label: t("intStateUnread", "Unread") }
                    ]
                }
            ],
            "gmail.archive": [connection("google"), text("messageId", "intMessageId", "Message id", "intMessageIdHelp")],
            "gmail.trash": [connection("google"), text("messageId", "intMessageId", "Message id", "intMessageIdHelp")],
            "gmail.label": [
                connection("google"),
                text("messageId", "intMessageId", "Message id", "intMessageIdHelp"),
                text("label", "intLabelName", "Label name", "", true),
                {
                    key: "mode",
                    type: "select",
                    label: t("intLabelMode", "What to do"),
                    options: [
                        { value: "add", label: t("intLabelAdd", "Add the label") },
                        { value: "remove", label: t("intLabelRemove", "Remove the label") }
                    ]
                }
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

            // ---------- GitHub ----------
            "github.list_repos": [
                connection("github"),
                text("nameContains", "intGhNameContains", "Name contains"),
                number("max", "intMaxResults", "Max results")
            ],
            "github.get_repo": [connection("github"), repo()],
            "github.create_issue": [
                connection("github"), repo(),
                text("title", "intGhTitle", "Title"),
                area("body", "intGhBody", "Text", "intVariablesHelp"),
                text("labels", "intGhLabels", "Labels", "intGhLabelsHelp"),
                text("assignees", "intGhAssignees", "Assignees (GitHub logins)", "intGhLabelsHelp")
            ],
            "github.get_issue": [connection("github"), repo(), numberField()],
            "github.list_issues": [
                connection("github"), repo(),
                choice("state", "intGhState", "State", [["open", "intGhOpen", "Open"], ["closed", "intGhClosed", "Closed"], ["all", "intGhAll", "All"]]),
                text("labels", "intGhLabels", "Labels", "intGhLabelsHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "github.update_issue": [
                connection("github"), repo(), numberField(),
                note("intGhUpdateHelp"),
                text("title", "intGhTitle", "Title"),
                area("body", "intGhBody", "Text", "intVariablesHelp"),
                choice("state", "intGhState", "State", [["", "intGhKeep", "Do not change"], ["open", "intGhOpen", "Open"], ["closed", "intGhClosed", "Closed"]]),
                text("labels", "intGhLabels", "Labels", "intGhLabelsHelp"),
                text("assignees", "intGhAssignees", "Assignees (GitHub logins)", "intGhLabelsHelp")
            ],
            "github.comment_issue": [
                connection("github"), repo(), numberField(),
                area("body", "intGhBody", "Text", "intVariablesHelp")
            ],
            "github.create_pr": [
                connection("github"), repo(),
                text("title", "intGhTitle", "Title"),
                text("head", "intGhHead", "Branch with the changes"),
                text("base", "intGhBase", "Merge into branch"),
                area("body", "intGhBody", "Text", "intVariablesHelp"),
                yesNo("draft", "intGhDraft", "Draft")
            ],
            "github.get_pr": [connection("github"), repo(), numberField()],
            "github.list_prs": [
                connection("github"), repo(),
                choice("state", "intGhState", "State", [["open", "intGhOpen", "Open"], ["closed", "intGhClosed", "Closed"], ["all", "intGhAll", "All"]]),
                text("base", "intGhBase", "Merge into branch"),
                number("max", "intMaxResults", "Max results")
            ],
            "github.merge_pr": [
                connection("github"), repo(), numberField(),
                {
                    key: "method",
                    type: "select",
                    label: t("intGhMergeMethod", "How to merge"),
                    options: [
                        { value: "merge", label: "Merge" },
                        { value: "squash", label: "Squash" },
                        { value: "rebase", label: "Rebase" }
                    ]
                },
                text("commitTitle", "intGhCommitTitle", "Merge commit title (optional)")
            ],
            "github.list_branches": [connection("github"), repo(), number("max", "intMaxResults", "Max results")],
            "github.list_commits": [
                connection("github"), repo(),
                text("branch", "intGhBranch", "Branch", "intGhBranchHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "github.create_branch": [
                connection("github"), repo(),
                text("name", "intGhNewBranch", "New branch name"),
                text("from", "intGhFrom", "Create from branch", "intGhBranchHelp")
            ],
            "github.get_file": [
                connection("github"), repo(),
                text("path", "intGhPath", "File path", "intGhPathHelp"),
                text("ref", "intGhRef", "Branch or tag", "intGhBranchHelp")
            ],
            "github.put_file": [
                connection("github"), repo(),
                note("intGhPutHelp"),
                text("path", "intGhPath", "File path", "intGhPathHelp"),
                area("text", "intGhFileText", "File content", "intVariablesHelp"),
                text("message", "intGhCommitMessage", "Commit message"),
                text("branch", "intGhBranch", "Branch", "intGhBranchHelp")
            ],
            "github.create_release": [
                connection("github"), repo(),
                text("tag", "intGhTag", "Tag"),
                text("name", "intGhReleaseName", "Release name"),
                area("body", "intGhBody", "Text", "intVariablesHelp"),
                text("target", "intGhTarget", "Branch or commit for the tag", "intGhBranchHelp"),
                yesNo("draft", "intGhDraft", "Draft"),
                yesNo("prerelease", "intGhPrerelease", "Pre-release")
            ],
            "github.dispatch_workflow": [
                connection("github"), repo(),
                text("workflow", "intGhWorkflow", "Workflow file", "intGhWorkflowHelp"),
                text("ref", "intGhRef", "Branch or tag", "intGhBranchHelp"),
                area("inputs", "intGhInputs", "Inputs (JSON, optional)", "intVariablesHelp")
            ],
            "github.list_workflow_runs": [
                connection("github"), repo(),
                choice("status", "intGhRunStatus", "Result", [["", "intGhAnyStatus", "Any"], ["failure", "intGhFailed", "Failed"], ["success", "intGhSucceeded", "Succeeded"]]),
                text("branch", "intGhBranch", "Branch", "intGhBranchHelp"),
                number("max", "intMaxResults", "Max results")
            ],
            "github.api_request": [
                connection("github"),
                note("intGhApiNote"),
                choice("method", "intGhMethod", "Method", [["GET", null, "GET"], ["POST", null, "POST"], ["PUT", null, "PUT"], ["PATCH", null, "PATCH"], ["DELETE", null, "DELETE"]]),
                text("path", "intGhApiPath", "API path", "intGhApiPathHelp", true),
                area("body", "intGhApiBody", "Body (JSON, optional)", "intVariablesHelp")
            ],
            "github.issue_opened": [
                connection("github"), repo(),
                text("titleContains", "intGhTitleContains", "Title contains"),
                note("intGhTriggerHelp")
            ],
            "github.pr_opened": [
                connection("github"), repo(),
                text("titleContains", "intGhTitleContains", "Title contains"),
                note("intGhTriggerHelp")
            ],
            "github.push": [
                connection("github"), repo(),
                text("branch", "intGhBranch", "Branch", "intGhBranchHelp"),
                note("intGhPushHelp"),
                note("intGhTriggerHelp")
            ],
            "github.release_published": [connection("github"), repo(), note("intGhTriggerHelp")],
            "github.workflow_failed": [
                connection("github"), repo(),
                text("branch", "intGhBranch", "Branch", "intGhBranchHelp"),
                note("intGhTriggerHelp")
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

            "drive.get_file": [connection("google"), text("fileId", "intFileId", "File or folder", "intFileIdHelp")],
            "drive.copy_file": [
                connection("google"),
                text("fileId", "intFileId", "File or folder", "intFileIdHelp"),
                text("name", "intCopyName", "Name of the copy (optional)"),
                text("folderId", "intDestFolder", "Destination folder", "intFolderIdHelp")
            ],
            "drive.move_file": [
                connection("google"),
                text("fileId", "intFileId", "File or folder", "intFileIdHelp"),
                text("folderId", "intDestFolder", "Destination folder", "intFolderIdHelp")
            ],
            "drive.rename_file": [
                connection("google"),
                text("fileId", "intFileId", "File or folder", "intFileIdHelp"),
                text("name", "intNewName", "New name")
            ],
            "drive.trash_file": [connection("google"), text("fileId", "intFileId", "File or folder", "intFileIdHelp")],
            "drive.share_file": [
                connection("google"),
                text("fileId", "intFileId", "File or folder", "intFileIdHelp"),
                text("email", "intShareEmail", "Person's email"),
                {
                    key: "role",
                    type: "select",
                    label: t("intShareRole", "Access"),
                    options: [
                        { value: "reader", label: t("intRoleReader", "Can view") },
                        { value: "commenter", label: t("intRoleCommenter", "Can comment") },
                        { value: "writer", label: t("intRoleWriter", "Can edit") }
                    ]
                },
                {
                    key: "notify",
                    type: "select",
                    label: t("intNotifyPerson", "Email the person"),
                    options: [
                        { value: "true", label: t("intYes", "Yes") },
                        { value: "false", label: t("intNo", "No") }
                    ]
                }
            ],
            "drive.create_text_file": [
                connection("google"),
                text("name", "intFileName", "File name"),
                area("content", "intFileContent", "Content", "intVariablesHelp"),
                text("folderId", "intFolderId", "Folder id", "intFolderIdHelp"),
                {
                    key: "asDocument",
                    type: "select",
                    label: t("intCreateAs", "Create as"),
                    options: [
                        { value: "false", label: t("intAsTextFile", "Text file") },
                        { value: "true", label: t("intAsGoogleDoc", "Google Doc") }
                    ]
                }
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
            ],

            // Computer cards live in builder-forms-computer.js
        };

        var helpers = { connection: connection, text: text, area: area, number: number, yesNo: yesNo, note: note };
        if (typeof window.partnerlyComputerForms === "function") {
            Object.assign(forms, window.partnerlyComputerForms(t, helpers));
        }

        return forms;
    };
})();
