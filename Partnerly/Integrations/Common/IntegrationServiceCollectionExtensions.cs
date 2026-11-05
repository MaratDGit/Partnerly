using Partnerly.Automation.Abstractions;
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
            services.AddScoped<WhatsAppClient>();
            services.AddScoped<WhatsAppWebhookService>();

            services.AddTransient<TelegramMessageTrigger>();
            services.AddTransient<SheetsRowAddedTrigger>();
            services.AddTransient<GmailMessageTrigger>();
            services.AddTransient<DriveFileAddedTrigger>();
            services.AddTransient<WhatsAppMessageTrigger>();
            services.AddTransient<OutlookMessageTrigger>();
            services.AddTransient<OutlookEventTrigger>();

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
            services.AddTransient<WhatsAppSendTextAction>();
            services.AddTransient<WhatsAppSendTemplateAction>();
            services.AddTransient<OutlookSendAction>();
            services.AddTransient<OutlookFindMessagesAction>();
            services.AddTransient<OutlookCreateEventAction>();
            services.AddTransient<OutlookFindEventsAction>();

            services.AddScoped<IIntegrationPoller, TelegramPoller>();
            services.AddScoped<IIntegrationPoller, SheetsPoller>();
            services.AddScoped<IIntegrationPoller, GmailPoller>();
            services.AddScoped<IIntegrationPoller, DrivePoller>();
            services.AddScoped<IIntegrationPoller, OutlookPoller>();
            services.AddScoped<IIntegrationPoller, OutlookEventPoller>();

            if (hostPolling)
            {
                services.AddHostedService<IntegrationPollingWorker>();
            }

            return services;
        }
    }
}
