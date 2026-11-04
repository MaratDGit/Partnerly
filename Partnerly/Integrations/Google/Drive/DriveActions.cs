using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Drive
{
    public abstract class DriveActionBase : IntegrationActionHandlerBase
    {
        protected DriveActionBase(GoogleDriveApi drive)
        {
            Drive = drive;
        }

        protected GoogleDriveApi Drive { get; }

        public override string Group => "Google Drive";
        public override string ConnectionKind => IntegrationKinds.Google;
    }

    public class DriveUploadFileAction : DriveActionBase
    {
        public DriveUploadFileAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveUploadFile;
        public override string DisplayName => "Upload file to Google Drive";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var file = await IntegrationFiles.OpenAttachmentAsync(context, Read(context, binder, "attachmentId"), cancellationToken);
            if (file == null)
            {
                return StepResult.Failed("Attachment was not found.", retryable: false);
            }

            await using (file.Stream)
            {
                var json = await Drive.UploadAsync(connection, file, Read(context, binder, "fileName"), Read(context, binder, "folderId"), cancellationToken);
                return StepResult.Succeeded(GoogleDriveApi.Clean(json));
            }
        }
    }

    public class DriveCreateFolderAction : DriveActionBase
    {
        public DriveCreateFolderAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveCreateFolder;
        public override string DisplayName => "Create Google Drive folder";

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var name = Read(context, binder, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                return StepResult.Failed("Folder name is required.", retryable: false);
            }

            var json = await Drive.CreateFolderAsync(connection, name, Read(context, binder, "parentId"), cancellationToken);
            return StepResult.Succeeded(GoogleDriveApi.Clean(json));
        }
    }

    public class DriveFindFilesAction : DriveActionBase
    {
        public DriveFindFilesAction(GoogleDriveApi drive) : base(drive) { }

        public override string Type => IntegrationTypeCodes.DriveFindFiles;
        public override string DisplayName => "Find files in Google Drive";
        protected override bool Writes => false;

        protected override async Task<StepResult> ExecuteCoreAsync(
            ActionContext context,
            IntegrationConnection connection,
            IVariableBinder binder,
            CancellationToken cancellationToken)
        {
            var max = int.TryParse(Read(context, binder, "max"), out var value) ? value : 10;
            var files = await Drive.FindAsync(connection, Read(context, binder, "nameContains"), Read(context, binder, "folderId"), max, cancellationToken);
            var array = new JsonArray(files.Select(item => (JsonNode?)item).ToArray());
            return StepResult.Succeeded(new JsonObject
            {
                ["count"] = files.Count,
                ["first"] = files.Count > 0 ? files[0].DeepClone() : null,
                ["files"] = array
            });
        }
    }
}
