using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Drive
{
    public abstract class DriveItemActionBase : DriveActionBase
    {
        protected DriveItemActionBase(GoogleDriveApi drive) : base(drive) { }

        protected static string FileId(ActionContext context, IVariableBinder binder) =>
            GoogleDriveApi.ItemId(Read(context, binder, "fileId"));

        protected static StepResult NeedFile() => StepResult.Failed("File id is required.", retryable: false);
    }

    public class DriveGetFileAction : DriveItemActionBase
    {
        public DriveGetFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveGetFile;
        public override string DisplayName => "Get Google Drive file info";
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            if (string.IsNullOrWhiteSpace(id))
            {
                return NeedFile();
            }

            return StepResult.Succeeded(await Drive.GetFileAsync(connection, id, cancellationToken));
        }
    }

    public class DriveCopyFileAction : DriveItemActionBase
    {
        public DriveCopyFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveCopyFile;
        public override string DisplayName => "Copy Google Drive file";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            if (string.IsNullOrWhiteSpace(id))
            {
                return NeedFile();
            }

            return StepResult.Succeeded(await Drive.CopyFileAsync(
                connection,
                id,
                Read(context, binder, "name"),
                GoogleDriveApi.ItemId(Read(context, binder, "folderId")),
                cancellationToken));
        }
    }

    public class DriveMoveFileAction : DriveItemActionBase
    {
        public DriveMoveFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveMoveFile;
        public override string DisplayName => "Move Google Drive file";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            var folder = GoogleDriveApi.ItemId(Read(context, binder, "folderId"));
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(folder))
            {
                return StepResult.Failed("File id and destination folder are required.", retryable: false);
            }

            return StepResult.Succeeded(await Drive.MoveFileAsync(connection, id, folder, cancellationToken));
        }
    }

    public class DriveRenameFileAction : DriveItemActionBase
    {
        public DriveRenameFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveRenameFile;
        public override string DisplayName => "Rename Google Drive file";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            var name = Read(context, binder, "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                return StepResult.Failed("File id and the new name are required.", retryable: false);
            }

            return StepResult.Succeeded(await Drive.UpdateFileAsync(connection, id, new JsonObject { ["name"] = name }, cancellationToken));
        }
    }

    // Moves to the Drive trash (restorable for 30 days) - never deletes for good.
    public class DriveTrashFileAction : DriveItemActionBase
    {
        public DriveTrashFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveTrashFile;
        public override string DisplayName => "Move Google Drive file to trash";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            if (string.IsNullOrWhiteSpace(id))
            {
                return NeedFile();
            }

            return StepResult.Succeeded(await Drive.UpdateFileAsync(connection, id, new JsonObject { ["trashed"] = true }, cancellationToken));
        }
    }

    public class DriveShareFileAction : DriveItemActionBase
    {
        private static readonly string[] Roles = { "reader", "commenter", "writer" };

        public DriveShareFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveShareFile;
        public override string DisplayName => "Share Google Drive file";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var id = FileId(context, binder);
            var email = Read(context, binder, "email");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return StepResult.Failed("File id and the person's email are required.", retryable: false);
            }

            var role = Read(context, binder, "role").ToLowerInvariant();
            if (!Roles.Contains(role))
            {
                role = "reader";
            }

            var permission = await Drive.ShareFileAsync(connection, id, email, role, Read(context, binder, "notify") != "false", cancellationToken);
            var file = await Drive.GetFileAsync(connection, id, cancellationToken);
            return StepResult.Succeeded(new JsonObject
            {
                ["fileId"] = id,
                ["email"] = email,
                ["role"] = permission["role"]?.ToString() ?? role,
                ["permissionId"] = permission["id"]?.ToString(),
                ["webViewLink"] = file["webViewLink"]?.ToString()
            });
        }
    }

    public class DriveCreateTextFileAction : DriveActionBase
    {
        public DriveCreateTextFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveCreateTextFile;
        public override string DisplayName => "Create Google Drive text file";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var name = Read(context, binder, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                return StepResult.Failed("File name is required.", retryable: false);
            }

            // "Read" trims; the text itself is kept as typed apart from variables.
            var content = binder.Interpolate(NodeConfig.GetString(context.Node, "content"), context.Variables);
            return StepResult.Succeeded(await Drive.CreateTextFileAsync(
                connection,
                name,
                content,
                GoogleDriveApi.ItemId(Read(context, binder, "folderId")),
                Read(context, binder, "asDocument") == "true",
                cancellationToken));
        }
    }
}
