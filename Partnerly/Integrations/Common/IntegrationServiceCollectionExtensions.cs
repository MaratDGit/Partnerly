using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.GitHub;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.Google.Drive;
using Partnerly.Integrations.Google.Gmail;
using Partnerly.Integrations.Google.Sheets;
using Partnerly.Integrations.Microsoft;
using Partnerly.Integrations.Microsoft.Outlook;
using Partnerly.Integrations.Telegram;
using Partnerly.Integrations.WhatsApp;

namespace Partnerly.Integrations.Common
{
    public static class IntegrationServiceCollectionExtensions
    {
        // Call after AddPartnerlyAutomation. hostPolling = false in unit tests.
        public static IServiceCollection AddPartnerlyIntegrations(this IServiceCollection services, bool hostPolling = true)
        {
            // HttpClient's default logging writes full request URLs at Information;
            // the Telegram bot token is part of the URL, so keep these categories at Warning.
            services.AddLogging(logging =>
                logging.AddFilter("System.Net.Http.HttpClient." + IntegrationHttp.ClientName, LogLevel.Warning));

            services.AddHttpClient(IntegrationHttp.ClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

            services.AddSingleton<IAutomationModule, IntegrationsAutomationModule>();
            services.AddScoped<IIntegrationConnectionService, IntegrationConnectionService>();
            services.AddScoped<IntegrationPollingService>();

            services.AddScoped<TelegramClient>();
            services.AddScoped<GoogleOAuthService>();
            services.AddScoped<GoogleApiClient>();
            services.AddScoped<GoogleSheetsApi>();
            services.AddScoped<GmailApi>();
            services.AddScoped<GoogleDriveApi>();
            services.AddScoped<MicrosoftOAuthService>();
            services.AddScoped<MicrosoftApiClient>();
            services.AddScoped<OutlookApi>();
            services.AddScoped<GitHubOAuthService>();
            services.AddScoped<GitHubApiClient>();
            services.AddScoped<WhatsAppClient>();
            services.AddScoped<WhatsAppWebhookService>();

            services.AddTransient<TelegramMessageTrigger>();
            services.AddTransient<SheetsRowAddedTrigger>();
            services.AddTransient<GmailMessageTrigger>();
            services.AddTransient<DriveFileAddedTrigger>();
            services.AddTransient<WhatsAppMessageTrigger>();
            services.AddTransient<OutlookMessageTrigger>();
            services.AddTransient<OutlookEventTrigger>();
            services.AddTransient<GitHubIssueOpenedTrigger>();
            services.AddTransient<GitHubPullRequestOpenedTrigger>();
            services.AddTransient<GitHubPushTrigger>();
            services.AddTransient<GitHubReleasePublishedTrigger>();
            services.AddTransient<GitHubWorkflowFailedTrigger>();

            services.AddTransient<TelegramSendMessageAction>();
            services.AddTransient<TelegramSendDocumentAction>();
            services.AddTransient<SheetsAppendRowAction>();
            services.AddTransient<SheetsFindRowsAction>();
            services.AddTransient<SheetsUpdateRowAction>();
            services.AddTransient<GmailSendAction>();
            services.AddTransient<GmailFindMessagesAction>();
            services.AddTransient<DriveUploadFileAction>();
            services.AddTransient<DriveCreateFolderAction>();
            services.AddTransient<DriveFindFilesAction>();
            services.AddTransient<GmailReplyAction>();
            services.AddTransient<GmailGetMessageAction>();
            services.AddTransient<GmailCreateDraftAction>();
            services.AddTransient<GmailMarkReadAction>();
            services.AddTransient<GmailArchiveAction>();
            services.AddTransient<GmailTrashAction>();
            services.AddTransient<GmailLabelAction>();
            services.AddTransient<SheetsGetRowAction>();
            services.AddTransient<SheetsReadRangeAction>();
            services.AddTransient<SheetsUpdateCellAction>();
            services.AddTransient<SheetsClearRangeAction>();
            services.AddTransient<SheetsDeleteRowAction>();
            services.AddTransient<SheetsCreateSpreadsheetAction>();
            services.AddTransient<SheetsAddSheetAction>();
            services.AddTransient<DriveGetFileAction>();
            services.AddTransient<DriveCopyFileAction>();
            services.AddTransient<DriveMoveFileAction>();
            services.AddTransient<DriveRenameFileAction>();
            services.AddTransient<DriveTrashFileAction>();
            services.AddTransient<DriveShareFileAction>();
            services.AddTransient<DriveCreateTextFileAction>();
            services.AddTransient<WhatsAppSendTextAction>();
            services.AddTransient<WhatsAppSendTemplateAction>();
            services.AddTransient<OutlookSendAction>();
            services.AddTransient<OutlookFindMessagesAction>();
            services.AddTransient<OutlookCreateEventAction>();
            services.AddTransient<OutlookFindEventsAction>();
            services.AddTransient<GitHubListReposAction>();
            services.AddTransient<GitHubGetRepoAction>();
            services.AddTransient<GitHubCreateIssueAction>();
            services.AddTransient<GitHubGetIssueAction>();
            services.AddTransient<GitHubListIssuesAction>();
            services.AddTransient<GitHubUpdateIssueAction>();
            services.AddTransient<GitHubCommentIssueAction>();
            services.AddTransient<GitHubCreatePullRequestAction>();
            services.AddTransient<GitHubGetPullRequestAction>();
            services.AddTransient<GitHubListPullRequestsAction>();
            services.AddTransient<GitHubMergePullRequestAction>();
            services.AddTransient<GitHubListBranchesAction>();
            services.AddTransient<GitHubListCommitsAction>();
            services.AddTransient<GitHubCreateBranchAction>();
            services.AddTransient<GitHubGetFileAction>();
            services.AddTransient<GitHubPutFileAction>();
            services.AddTransient<GitHubCreateReleaseAction>();
            services.AddTransient<GitHubDispatchWorkflowAction>();
            services.AddTransient<GitHubListWorkflowRunsAction>();
            services.AddTransient<GitHubApiRequestAction>();

            services.AddScoped<IIntegrationPoller, TelegramPoller>();
            services.AddScoped<IIntegrationPoller, SheetsPoller>();
            services.AddScoped<IIntegrationPoller, GmailPoller>();
            services.AddScoped<IIntegrationPoller, DrivePoller>();
            services.AddScoped<IIntegrationPoller, OutlookPoller>();
            services.AddScoped<IIntegrationPoller, OutlookEventPoller>();
            services.AddScoped<IIntegrationPoller, GitHubIssueOpenedPoller>();
            services.AddScoped<IIntegrationPoller, GitHubPullRequestOpenedPoller>();
            services.AddScoped<IIntegrationPoller, GitHubPushPoller>();
            services.AddScoped<IIntegrationPoller, GitHubReleasePublishedPoller>();
            services.AddScoped<IIntegrationPoller, GitHubWorkflowFailedPoller>();

            if (hostPolling)
            {
                services.AddHostedService<IntegrationPollingWorker>();
            }

            return services;
        }
    }
}
