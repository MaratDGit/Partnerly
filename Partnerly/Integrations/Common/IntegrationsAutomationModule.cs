using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Registry;
using Partnerly.Integrations.Google.Drive;
using Partnerly.Integrations.Google.Gmail;
using Partnerly.Integrations.Google.Sheets;
using Partnerly.Integrations.Microsoft.Outlook;
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
            registry.Register<OutlookMessageTrigger>(IntegrationTypeCodes.OutlookMessageReceived, IntegrationKinds.Microsoft);
            registry.Register<OutlookEventTrigger>(IntegrationTypeCodes.OutlookEventAdded, IntegrationKinds.Microsoft);
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
            registry.Register<GmailReplyAction>(IntegrationTypeCodes.GmailReply, IntegrationKinds.Google);
            registry.Register<GmailGetMessageAction>(IntegrationTypeCodes.GmailGetMessage, IntegrationKinds.Google);
            registry.Register<GmailCreateDraftAction>(IntegrationTypeCodes.GmailCreateDraft, IntegrationKinds.Google);
            registry.Register<GmailMarkReadAction>(IntegrationTypeCodes.GmailMarkRead, IntegrationKinds.Google);
            registry.Register<GmailArchiveAction>(IntegrationTypeCodes.GmailArchive, IntegrationKinds.Google);
            registry.Register<GmailTrashAction>(IntegrationTypeCodes.GmailTrash, IntegrationKinds.Google);
            registry.Register<GmailLabelAction>(IntegrationTypeCodes.GmailLabel, IntegrationKinds.Google);
            registry.Register<SheetsGetRowAction>(IntegrationTypeCodes.SheetsGetRow, IntegrationKinds.Google);
            registry.Register<SheetsReadRangeAction>(IntegrationTypeCodes.SheetsReadRange, IntegrationKinds.Google);
            registry.Register<SheetsUpdateCellAction>(IntegrationTypeCodes.SheetsUpdateCell, IntegrationKinds.Google);
            registry.Register<SheetsClearRangeAction>(IntegrationTypeCodes.SheetsClearRange, IntegrationKinds.Google);
            registry.Register<SheetsDeleteRowAction>(IntegrationTypeCodes.SheetsDeleteRow, IntegrationKinds.Google);
            registry.Register<SheetsCreateSpreadsheetAction>(IntegrationTypeCodes.SheetsCreateSpreadsheet, IntegrationKinds.Google);
            registry.Register<SheetsAddSheetAction>(IntegrationTypeCodes.SheetsAddSheet, IntegrationKinds.Google);
            registry.Register<DriveGetFileAction>(IntegrationTypeCodes.DriveGetFile, IntegrationKinds.Google);
            registry.Register<DriveCopyFileAction>(IntegrationTypeCodes.DriveCopyFile, IntegrationKinds.Google);
            registry.Register<DriveMoveFileAction>(IntegrationTypeCodes.DriveMoveFile, IntegrationKinds.Google);
            registry.Register<DriveRenameFileAction>(IntegrationTypeCodes.DriveRenameFile, IntegrationKinds.Google);
            registry.Register<DriveTrashFileAction>(IntegrationTypeCodes.DriveTrashFile, IntegrationKinds.Google);
            registry.Register<DriveShareFileAction>(IntegrationTypeCodes.DriveShareFile, IntegrationKinds.Google);
            registry.Register<DriveCreateTextFileAction>(IntegrationTypeCodes.DriveCreateTextFile, IntegrationKinds.Google);
            registry.Register<WhatsAppSendTextAction>(IntegrationTypeCodes.WhatsAppSendText, IntegrationKinds.WhatsApp);
            registry.Register<WhatsAppSendTemplateAction>(IntegrationTypeCodes.WhatsAppSendTemplate, IntegrationKinds.WhatsApp);
            registry.Register<OutlookSendAction>(IntegrationTypeCodes.OutlookSend, IntegrationKinds.Microsoft);
            registry.Register<OutlookFindMessagesAction>(IntegrationTypeCodes.OutlookFindMessages, IntegrationKinds.Microsoft);
            registry.Register<OutlookCreateEventAction>(IntegrationTypeCodes.OutlookCreateEvent, IntegrationKinds.Microsoft);
            registry.Register<OutlookFindEventsAction>(IntegrationTypeCodes.OutlookFindEvents, IntegrationKinds.Microsoft);
        }
    }
}
