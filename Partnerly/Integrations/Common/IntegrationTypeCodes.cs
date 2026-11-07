namespace Partnerly.Integrations.Common
{
    public static class IntegrationTypeCodes
    {
        public const string TelegramSendMessage = "telegram.send_message";
        public const string TelegramSendDocument = "telegram.send_document";
        public const string TelegramMessageReceived = "telegram.message_received";

        public const string SheetsAppendRow = "sheets.append_row";
        public const string SheetsFindRows = "sheets.find_rows";
        public const string SheetsUpdateRow = "sheets.update_row";
        public const string SheetsRowAdded = "sheets.row_added";
        public const string SheetsGetRow = "sheets.get_row";
        public const string SheetsReadRange = "sheets.read_range";
        public const string SheetsUpdateCell = "sheets.update_cell";
        public const string SheetsClearRange = "sheets.clear_range";
        public const string SheetsDeleteRow = "sheets.delete_row";
        public const string SheetsCreateSpreadsheet = "sheets.create_spreadsheet";
        public const string SheetsAddSheet = "sheets.add_sheet";

        public const string GmailSend = "gmail.send";
        public const string GmailFindMessages = "gmail.find_messages";
        public const string GmailMessageReceived = "gmail.message_received";
        public const string GmailReply = "gmail.reply";
        public const string GmailGetMessage = "gmail.get_message";
        public const string GmailCreateDraft = "gmail.create_draft";
        public const string GmailMarkRead = "gmail.mark_read";
        public const string GmailArchive = "gmail.archive";
        public const string GmailTrash = "gmail.trash";
        public const string GmailLabel = "gmail.label";

        public const string DriveUploadFile = "drive.upload_file";
        public const string DriveCreateFolder = "drive.create_folder";
        public const string DriveFindFiles = "drive.find_files";
        public const string DriveFileAdded = "drive.file_added";
        public const string DriveGetFile = "drive.get_file";
        public const string DriveCopyFile = "drive.copy_file";
        public const string DriveMoveFile = "drive.move_file";
        public const string DriveRenameFile = "drive.rename_file";
        public const string DriveTrashFile = "drive.trash_file";
        public const string DriveShareFile = "drive.share_file";
        public const string DriveCreateTextFile = "drive.create_text_file";

        public const string WhatsAppSendText = "whatsapp.send_text";
        public const string WhatsAppSendTemplate = "whatsapp.send_template";
        public const string WhatsAppMessageReceived = "whatsapp.message_received";

        public const string OutlookSend = "outlook.send";
        public const string OutlookFindMessages = "outlook.find_messages";
        public const string OutlookMessageReceived = "outlook.message_received";
        public const string OutlookCreateEvent = "outlook.create_event";
        public const string OutlookFindEvents = "outlook.find_events";
        public const string OutlookEventAdded = "outlook.event_added";

        public const string GitHubListRepos = "github.list_repos";
        public const string GitHubGetRepo = "github.get_repo";
        public const string GitHubCreateIssue = "github.create_issue";
        public const string GitHubGetIssue = "github.get_issue";
        public const string GitHubListIssues = "github.list_issues";
        public const string GitHubUpdateIssue = "github.update_issue";
        public const string GitHubCommentIssue = "github.comment_issue";
        public const string GitHubCreatePullRequest = "github.create_pr";
        public const string GitHubGetPullRequest = "github.get_pr";
        public const string GitHubListPullRequests = "github.list_prs";
        public const string GitHubMergePullRequest = "github.merge_pr";
        public const string GitHubListBranches = "github.list_branches";
        public const string GitHubListCommits = "github.list_commits";
        public const string GitHubCreateBranch = "github.create_branch";
        public const string GitHubGetFile = "github.get_file";
        public const string GitHubPutFile = "github.put_file";
        public const string GitHubCreateRelease = "github.create_release";
        public const string GitHubDispatchWorkflow = "github.dispatch_workflow";
        public const string GitHubListWorkflowRuns = "github.list_workflow_runs";
        public const string GitHubApiRequest = "github.api_request";
        public const string GitHubIssueOpened = "github.issue_opened";
        public const string GitHubPullRequestOpened = "github.pr_opened";
        public const string GitHubPush = "github.push";
        public const string GitHubReleasePublished = "github.release_published";
        public const string GitHubWorkflowFailed = "github.workflow_failed";
    }
}
