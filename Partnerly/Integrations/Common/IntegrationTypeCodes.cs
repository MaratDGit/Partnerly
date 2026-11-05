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

        public const string GmailSend = "gmail.send";
        public const string GmailFindMessages = "gmail.find_messages";
        public const string GmailMessageReceived = "gmail.message_received";

        public const string DriveUploadFile = "drive.upload_file";
        public const string DriveCreateFolder = "drive.create_folder";
        public const string DriveFindFiles = "drive.find_files";
        public const string DriveFileAdded = "drive.file_added";

        public const string WhatsAppSendText = "whatsapp.send_text";
        public const string WhatsAppSendTemplate = "whatsapp.send_template";
        public const string WhatsAppMessageReceived = "whatsapp.message_received";

        public const string OutlookSend = "outlook.send";
        public const string OutlookFindMessages = "outlook.find_messages";
        public const string OutlookMessageReceived = "outlook.message_received";
        public const string OutlookCreateEvent = "outlook.create_event";
        public const string OutlookFindEvents = "outlook.find_events";
        public const string OutlookEventAdded = "outlook.event_added";
    }
}
