namespace Partnerly.Automation.Abstractions
{
    public static class AutomationTypeCodes
    {
        public const string Manual = "manual";
        public const string Schedule = "schedule";
        public const string Interval = "interval";
        public const string EntityCreated = "entity.created";
        public const string EntityUpdated = "entity.updated";
        public const string EntityDeleted = "entity.deleted";
        public const string Webhook = "webhook";
        public const string Event = "event";
        public const string DelayTrigger = "delay";

        public const string SendEmail = "send_email";
        public const string SendNotification = "send_notification";
        public const string CreateRecord = "create_record";
        public const string UpdateRecord = "update_record";
        public const string FindRecord = "find_record";
        public const string DeleteRecord = "delete_record";
        public const string Delay = "delay";
        public const string WaitUntil = "wait_until";
        public const string Branch = "branch";
        public const string Switch = "switch";
        public const string SetVariable = "set_variable";
        public const string HttpRequest = "http_request";
        public const string WebhookAction = "webhook";
        public const string ExecuteWorkflow = "execute_workflow";
        public const string Loop = "loop";
        public const string Filter = "filter";
        public const string StoreSet = "store_set";
        public const string StoreGet = "store_get";
        public const string StoreDelete = "store_delete";
        public const string Once = "once";

        public static readonly string[] Triggers =
        {
            Manual, Schedule, Interval, EntityCreated, EntityUpdated, EntityDeleted, Webhook, Event, DelayTrigger
        };

        public static readonly string[] Actions =
        {
            SendEmail, SendNotification, CreateRecord, UpdateRecord, FindRecord, DeleteRecord,
            Delay, WaitUntil, Branch, Switch, SetVariable, HttpRequest, WebhookAction, ExecuteWorkflow, Loop, Filter, StoreSet, StoreGet, StoreDelete, Once
        };
    }
}
