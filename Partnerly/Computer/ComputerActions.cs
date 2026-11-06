using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Integrations.Common;

namespace Partnerly.Computer
{
    // Base of every "Computer" card. The card only describes WHAT to do; the Partnerly Agent
    // on the PC decides whether it is allowed (folders, programs) and does it.
    public abstract class ComputerActionBase : IntegrationActionHandlerBase
    {
        public const long MaxFileBytes = 8L * 1024 * 1024;

        private readonly IComputerGateway _gateway;

        protected ComputerActionBase(IComputerGateway gateway)
        {
            _gateway = gateway;
        }

        public override string Group => "Computer";
        public override string ConnectionKind => IntegrationKinds.Computer;

        // Name of the command the agent understands.
        protected abstract string Op { get; }

        // Settings of the card that are passed to the agent as they are (variables already filled in).
        protected virtual string[] Fields => Array.Empty<string>();

        protected virtual TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromSeconds(60);

        // Lets an action add data before sending (for example the bytes of an attachment).
        // Returns an error text when the card cannot be run.
        protected virtual Task<string?> PrepareAsync(ActionContext context, JsonObject args, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        // Lets an action turn big data of the answer (a file) into something small for the run.
        protected virtual Task<string?> FinishAsync(ActionContext context, JsonObject data, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var args = new JsonObject();
            foreach (var key in Fields)
            {
                args[key] = Read(context, binder, key);
            }

            var prepareError = await PrepareAsync(context, args, cancellationToken);
            if (prepareError != null)
            {
                return StepResult.Failed(prepareError, retryable: false);
            }

            ComputerResponse response;
            try
            {
                response = await _gateway.SendAsync(connection.Id, Op, args, TimeoutFor(args), cancellationToken);
            }
            catch (ComputerException ex)
            {
                throw new IntegrationException(LocalizedError.Mark(ex.Key, ex.Message), ex.Retryable);
            }

            if (!response.Ok)
            {
                // Texts of the PC itself stay as they are; texts of the site are translated.
                return StepResult.Failed(
                    response.ErrorKey != null
                        ? LocalizedError.Mark(response.ErrorKey, response.Error ?? string.Empty)
                        : response.Error ?? LocalizedError.Mark("ComputerErrRefused", "The computer refused the command."),
                    retryable: false);
            }

            var finishError = await FinishAsync(context, response.Data, cancellationToken);
            return finishError != null
                ? StepResult.Failed(finishError, retryable: false)
                : StepResult.Succeeded(response.Data);
        }

        // Stores bytes that came from the PC as a Partnerly attachment, so the next cards
        // (Telegram, Drive...) can use them through {{...attachmentId}}.
        protected static async Task<string?> StoreAttachmentAsync(ActionContext context, JsonObject data, CancellationToken cancellationToken)
        {
            var base64 = data["contentBase64"]?.ToString();
            data.Remove("contentBase64");
            if (string.IsNullOrEmpty(base64))
            {
                return "The computer did not send the file.";
            }

            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException) { return "The computer sent a damaged file."; }

            var attachments = context.Services.GetService<IAttachmentService>();
            if (attachments == null)
            {
                return "Attachments are not available.";
            }

            var fileName = data["fileName"]?.ToString();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "file.bin";
            }

            using var stream = new MemoryStream(bytes);
            var file = new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
            try
            {
                var attachment = await attachments.UploadAsync(file, "AutomationRun", context.Run.Id);
                data["attachmentId"] = attachment.Id.ToString();
                data["size"] = bytes.Length;
                return null;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return ex.Message;
            }
        }

        protected static TimeSpan SecondsPlus(JsonObject args, string key, int fallback, int extra)
        {
            var seconds = int.TryParse(args[key]?.ToString(), out var parsed) && parsed > 0 ? parsed : fallback;
            return TimeSpan.FromSeconds(Math.Min(seconds, 600) + extra);
        }
    }

    // ---------- Messages on the screen ----------
    public sealed class ComputerNotifyAction : ComputerActionBase
    {
        public ComputerNotifyAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Notify;
        public override string DisplayName => "Show notification on PC";
        protected override string Op => "notify";
        protected override string[] Fields => new[] { "title", "text" };
    }

    public sealed class ComputerMessageBoxAction : ComputerActionBase
    {
        public ComputerMessageBoxAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.MessageBox;
        public override string DisplayName => "Show message window on PC";
        protected override string Op => "message_box";
        protected override string[] Fields => new[] { "title", "text", "buttons" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(5);
    }

    public sealed class ComputerSpeakAction : ComputerActionBase
    {
        public ComputerSpeakAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Speak;
        public override string DisplayName => "Speak text on PC";
        protected override string Op => "speak";
        protected override string[] Fields => new[] { "text" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromSeconds(120);
    }

    // ---------- Opening, clipboard ----------
    public sealed class ComputerOpenAction : ComputerActionBase
    {
        public ComputerOpenAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Open;
        public override string DisplayName => "Open link, file or folder on PC";
        protected override string Op => "open";
        protected override string[] Fields => new[] { "target" };
    }

    public sealed class ComputerClipboardSetAction : ComputerActionBase
    {
        public ComputerClipboardSetAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ClipboardSet;
        public override string DisplayName => "Copy text to clipboard";
        protected override string Op => "clipboard_set";
        protected override string[] Fields => new[] { "text" };
    }

    public sealed class ComputerClipboardGetAction : ComputerActionBase
    {
        public ComputerClipboardGetAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ClipboardGet;
        public override string DisplayName => "Read clipboard text";
        protected override bool Writes => false;
        protected override string Op => "clipboard_get";
    }

    // ---------- Information ----------
    public sealed class ComputerSystemInfoAction : ComputerActionBase
    {
        public ComputerSystemInfoAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.SystemInfo;
        public override string DisplayName => "Get PC information";
        protected override bool Writes => false;
        protected override string Op => "system_info";
    }

    public sealed class ComputerScreenshotAction : ComputerActionBase
    {
        public ComputerScreenshotAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Screenshot;
        public override string DisplayName => "Take screenshot";
        protected override string Op => "screenshot";
        protected override string[] Fields => new[] { "screen" };
        protected override Task<string?> FinishAsync(ActionContext context, JsonObject data, CancellationToken cancellationToken) =>
            StoreAttachmentAsync(context, data, cancellationToken);
    }

    public sealed class ComputerProcessRunningAction : ComputerActionBase
    {
        public ComputerProcessRunningAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ProcessRunning;
        public override string DisplayName => "Check if a program is running";
        protected override bool Writes => false;
        protected override string Op => "process_running";
        protected override string[] Fields => new[] { "name" };
    }

    // ---------- Files ----------
    public sealed class ComputerReadFileAction : ComputerActionBase
    {
        public ComputerReadFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ReadFile;
        public override string DisplayName => "Read text file";
        protected override bool Writes => false;
        protected override string Op => "read_file";
        protected override string[] Fields => new[] { "path", "maxKb" };
    }

    public sealed class ComputerWriteFileAction : ComputerActionBase
    {
        public ComputerWriteFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.WriteFile;
        public override string DisplayName => "Write text file";
        protected override string Op => "write_file";
        protected override string[] Fields => new[] { "path", "text", "mode" };
    }

    public sealed class ComputerListFolderAction : ComputerActionBase
    {
        public ComputerListFolderAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ListFolder;
        public override string DisplayName => "List files in folder";
        protected override bool Writes => false;
        protected override string Op => "list_folder";
        protected override string[] Fields => new[] { "path", "pattern", "recursive" };
    }

    public sealed class ComputerCopyFileAction : ComputerActionBase
    {
        public ComputerCopyFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.CopyFile;
        public override string DisplayName => "Copy file";
        protected override string Op => "copy_file";
        protected override string[] Fields => new[] { "source", "destination", "overwrite" };
    }

    public sealed class ComputerMoveFileAction : ComputerActionBase
    {
        public ComputerMoveFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.MoveFile;
        public override string DisplayName => "Move or rename file";
        protected override string Op => "move_file";
        protected override string[] Fields => new[] { "source", "destination", "overwrite" };
    }

    public sealed class ComputerZipFolderAction : ComputerActionBase
    {
        public ComputerZipFolderAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ZipFolder;
        public override string DisplayName => "Zip folder";
        protected override string Op => "zip_folder";
        protected override string[] Fields => new[] { "source", "destination" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(5);
    }

    // Partnerly attachment -> file on the PC.
    public sealed class ComputerSaveAttachmentAction : ComputerActionBase
    {
        public ComputerSaveAttachmentAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.SaveAttachment;
        public override string DisplayName => "Save attachment to PC";
        protected override string Op => "save_file";
        protected override string[] Fields => new[] { "attachmentId", "folder", "fileName", "overwrite" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(2);

        protected override async Task<string?> PrepareAsync(ActionContext context, JsonObject args, CancellationToken cancellationToken)
        {
            var file = await IntegrationFiles.OpenAttachmentAsync(context, args["attachmentId"]?.ToString() ?? string.Empty, cancellationToken);
            if (file == null)
            {
                return "Attachment was not found.";
            }

            await using (file.Stream)
            {
                using var buffer = new MemoryStream();
                await file.Stream.CopyToAsync(buffer, cancellationToken);
                if (buffer.Length > MaxFileBytes)
                {
                    return "The attachment is bigger than 8 MB.";
                }

                args["contentBase64"] = Convert.ToBase64String(buffer.ToArray());
                if (string.IsNullOrWhiteSpace(args["fileName"]?.ToString()))
                {
                    args["fileName"] = file.FileName;
                }
            }

            args.Remove("attachmentId");
            return null;
        }
    }

    // File on the PC -> Partnerly attachment.
    public sealed class ComputerGetFileAction : ComputerActionBase
    {
        public ComputerGetFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.GetFile;
        public override string DisplayName => "Get file from PC";
        protected override string Op => "get_file";
        protected override string[] Fields => new[] { "path" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(2);
        protected override Task<string?> FinishAsync(ActionContext context, JsonObject data, CancellationToken cancellationToken) =>
            StoreAttachmentAsync(context, data, cancellationToken);
    }

    // ---------- Programs ----------
    public sealed class ComputerRunProgramAction : ComputerActionBase
    {
        public ComputerRunProgramAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.RunProgram;
        public override string DisplayName => "Run program";
        protected override string Op => "run_program";
        protected override string[] Fields => new[] { "path", "arguments", "wait", "timeoutSeconds" };
        protected override TimeSpan TimeoutFor(JsonObject args) => SecondsPlus(args, "timeoutSeconds", 60, 20);
    }

    public sealed class ComputerRunScriptAction : ComputerActionBase
    {
        public ComputerRunScriptAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.RunScript;
        public override string DisplayName => "Run script";
        protected override string Op => "run_script";
        protected override string[] Fields => new[] { "path", "arguments", "wait", "timeoutSeconds" };
        protected override TimeSpan TimeoutFor(JsonObject args) => SecondsPlus(args, "timeoutSeconds", 60, 20);
    }

    public sealed class ComputerCloseProgramAction : ComputerActionBase
    {
        public ComputerCloseProgramAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.CloseProgram;
        public override string DisplayName => "Close program";
        protected override string Op => "close_program";
        protected override string[] Fields => new[] { "name" };
    }

    // ---------- Computer ----------
    public sealed class ComputerLockAction : ComputerActionBase
    {
        public ComputerLockAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Lock;
        public override string DisplayName => "Lock PC";
        protected override string Op => "lock";
    }

    public sealed class ComputerSleepAction : ComputerActionBase
    {
        public ComputerSleepAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Sleep;
        public override string DisplayName => "Put PC to sleep";
        protected override string Op => "sleep";
    }

    public sealed class ComputerDisplayOffAction : ComputerActionBase
    {
        public ComputerDisplayOffAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.DisplayOff;
        public override string DisplayName => "Turn the screen off";
        protected override string Op => "display_off";
    }

    public sealed class ComputerShutdownAction : ComputerActionBase
    {
        public ComputerShutdownAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Shutdown;
        public override string DisplayName => "Shut down, restart or sign out";
        protected override string Op => "shutdown";
        protected override string[] Fields => new[] { "mode", "delaySeconds" };
    }

    public sealed class ComputerCancelShutdownAction : ComputerActionBase
    {
        public ComputerCancelShutdownAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.CancelShutdown;
        public override string DisplayName => "Cancel planned shutdown";
        protected override string Op => "cancel_shutdown";
    }

    // ---------- Sound ----------
    public sealed class ComputerVolumeAction : ComputerActionBase
    {
        public ComputerVolumeAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Volume;
        public override string DisplayName => "Change volume";
        protected override string Op => "volume";
        protected override string[] Fields => new[] { "action", "steps" };
    }

    public sealed class ComputerMediaAction : ComputerActionBase
    {
        public ComputerMediaAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Media;
        public override string DisplayName => "Music and video buttons";
        protected override string Op => "media";
        protected override string[] Fields => new[] { "action" };
    }

    // ---------- More information ----------
    public sealed class ComputerActiveWindowAction : ComputerActionBase
    {
        public ComputerActiveWindowAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ActiveWindow;
        public override string DisplayName => "Get the window in front";
        protected override bool Writes => false;
        protected override string Op => "active_window";
    }

    public sealed class ComputerListProgramsAction : ComputerActionBase
    {
        public ComputerListProgramsAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.ListPrograms;
        public override string DisplayName => "List open programs";
        protected override bool Writes => false;
        protected override string Op => "list_programs";
    }

    public sealed class ComputerNetworkInfoAction : ComputerActionBase
    {
        public ComputerNetworkInfoAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.NetworkInfo;
        public override string DisplayName => "Get network information";
        protected override bool Writes => false;
        protected override string Op => "network_info";
    }

    public sealed class ComputerPingHostAction : ComputerActionBase
    {
        public ComputerPingHostAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.PingHost;
        public override string DisplayName => "Ping a site or device";
        protected override bool Writes => false;
        protected override string Op => "ping_host";
        protected override string[] Fields => new[] { "host" };
    }

    // ---------- More files ----------
    public sealed class ComputerCreateFolderAction : ComputerActionBase
    {
        public ComputerCreateFolderAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.CreateFolder;
        public override string DisplayName => "Create folder";
        protected override string Op => "create_folder";
        protected override string[] Fields => new[] { "path" };
    }

    public sealed class ComputerDeleteFileAction : ComputerActionBase
    {
        public ComputerDeleteFileAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.DeleteFile;
        public override string DisplayName => "Move file to Recycle Bin";
        protected override string Op => "delete_file";
        protected override string[] Fields => new[] { "path" };
    }

    public sealed class ComputerFileInfoAction : ComputerActionBase
    {
        public ComputerFileInfoAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.FileInfo;
        public override string DisplayName => "Check if a file exists";
        protected override bool Writes => false;
        protected override string Op => "file_info";
        protected override string[] Fields => new[] { "path" };
    }

    public sealed class ComputerUnzipAction : ComputerActionBase
    {
        public ComputerUnzipAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.Unzip;
        public override string DisplayName => "Unzip archive";
        protected override string Op => "unzip";
        protected override string[] Fields => new[] { "source", "destination", "overwrite" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(5);
    }

    public sealed class ComputerDownloadUrlAction : ComputerActionBase
    {
        public ComputerDownloadUrlAction(IComputerGateway gateway) : base(gateway) { }
        public override string Type => ComputerTypeCodes.DownloadUrl;
        public override string DisplayName => "Download file from the internet";
        protected override string Op => "download_url";
        protected override string[] Fields => new[] { "url", "folder", "fileName", "overwrite" };
        protected override TimeSpan TimeoutFor(JsonObject args) => TimeSpan.FromMinutes(3);
    }
}
