using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Registry;
using Partnerly.Integrations.Google.Drive;
using Partnerly.Integrations.Google.Gmail;
using Partnerly.Integrations.Google.Sheets;
using Partnerly.Integrations.Telegram;
using Partnerly.Integrations.WhatsApp;

namespace Partnerly.Integrations.Common
{
    // Every integration node is registered with its connection kind, so
    // GraphValidator refuses to publish a step that has no connection chosen.
    public class IntegrationsAutomationModule : IAutomationModule
    {
        public void RegisterTriggers(TriggerRegistry registry)
        {
            registry.Register<TelegramMessageTrigger>(IntegrationTypeCodes.TelegramMessageReceived, IntegrationKinds.Telegram);
            registry.Register<SheetsRowAddedTrigger>(IntegrationTypeCodes.SheetsRowAdded, IntegrationKinds.Google);
            registry.Register<GmailMessageTrigger>(IntegrationTypeCodes.GmailMessageReceived, IntegrationKinds.Google);
            registry.Register<DriveFileAddedTrigger>(IntegrationTypeCodes.DriveFileAdded, IntegrationKinds.Google);
            registry.Register<WhatsAppMessageTrigger>(IntegrationTypeCodes.WhatsAppMessageReceived, IntegrationKinds.WhatsApp);
        }

        public void RegisterActions(ActionRegistry registry)
        {
            registry.Register<TelegramSendMessageAction>(IntegrationTypeCodes.TelegramSendMessage, IntegrationKinds.Telegram);
            registry.Register<TelegramSendDocumentAction>(IntegrationTypeCodes.TelegramSendDocument, IntegrationKinds.Telegram);
            registry.Register<SheetsAppendRowAction>(IntegrationTypeCodes.SheetsAppendRow, IntegrationKinds.Google);
            registry.Register<SheetsFindRowsAction>(IntegrationTypeCodes.SheetsFindRows, IntegrationKinds.Google);
            registry.Register<SheetsUpdateRowAction>(IntegrationTypeCodes.SheetsUpdateRow, IntegrationKinds.Google);
            registry.Register<GmailSendAction>(IntegrationTypeCodes.GmailSend, IntegrationKinds.Google);
            registry.Register<GmailFindMessagesAction>(IntegrationTypeCodes.GmailFindMessages, IntegrationKinds.Google);
            registry.Register<DriveUploadFileAction>(IntegrationTypeCodes.DriveUploadFile, IntegrationKinds.Google);
            registry.Register<DriveCreateFolderAction>(IntegrationTypeCodes.DriveCreateFolder, IntegrationKinds.Google);
            registry.Register<DriveFindFilesAction>(IntegrationTypeCodes.DriveFindFiles, IntegrationKinds.Google);
            registry.Register<WhatsAppSendTextAction>(IntegrationTypeCodes.WhatsAppSendText, IntegrationKinds.WhatsApp);
            registry.Register<WhatsAppSendTemplateAction>(IntegrationTypeCodes.WhatsAppSendTemplate, IntegrationKinds.WhatsApp);
        }
    }
}
