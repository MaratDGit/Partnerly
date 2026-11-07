using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Registry;
using Partnerly.Integrations.Common;

namespace Partnerly.Computer
{
    public sealed class ComputerFileCreatedTrigger : IntegrationTriggerHandler
    {
        public ComputerFileCreatedTrigger()
            : base(ComputerTypeCodes.FileCreated, "New file on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerStatusTrigger : IntegrationTriggerHandler
    {
        public ComputerStatusTrigger()
            : base(ComputerTypeCodes.StatusChanged, "PC went online or offline", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerProgramStartedTrigger : IntegrationTriggerHandler
    {
        public ComputerProgramStartedTrigger()
            : base(ComputerTypeCodes.ProgramStarted, "Program started on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerFileChangedTrigger : IntegrationTriggerHandler
    {
        public ComputerFileChangedTrigger()
            : base(ComputerTypeCodes.FileChanged, "File changed on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerFileDeletedTrigger : IntegrationTriggerHandler
    {
        public ComputerFileDeletedTrigger()
            : base(ComputerTypeCodes.FileDeleted, "File deleted on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerProgramStoppedTrigger : IntegrationTriggerHandler
    {
        public ComputerProgramStoppedTrigger()
            : base(ComputerTypeCodes.ProgramStopped, "Program closed on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerSessionTrigger : IntegrationTriggerHandler
    {
        public ComputerSessionTrigger()
            : base(ComputerTypeCodes.SessionChanged, "PC locked, unlocked or woke up", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerClipboardTrigger : IntegrationTriggerHandler
    {
        public ComputerClipboardTrigger()
            : base(ComputerTypeCodes.ClipboardChanged, "Clipboard text changed", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerPowerTrigger : IntegrationTriggerHandler
    {
        public ComputerPowerTrigger()
            : base(ComputerTypeCodes.PowerChanged, "Charger or battery changed", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerDriveTrigger : IntegrationTriggerHandler
    {
        public ComputerDriveTrigger()
            : base(ComputerTypeCodes.DriveChanged, "USB drive plugged or removed", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerIdleTrigger : IntegrationTriggerHandler
    {
        public ComputerIdleTrigger()
            : base(ComputerTypeCodes.IdleChanged, "Nobody at the PC / back again", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerDiskLowTrigger : IntegrationTriggerHandler
    {
        public ComputerDiskLowTrigger()
            : base(ComputerTypeCodes.DiskLow, "Disk space is running out", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerGitNewCommitTrigger : IntegrationTriggerHandler
    {
        public ComputerGitNewCommitTrigger()
            : base(ComputerTypeCodes.GitNewCommit, "New commit in a Git folder on PC", "Computer", IntegrationKinds.Computer) { }
    }

    public sealed class ComputerGitChangedTrigger : IntegrationTriggerHandler
    {
        public ComputerGitChangedTrigger()
            : base(ComputerTypeCodes.GitChanged, "Git folder on PC has changes / is clean again", "Computer", IntegrationKinds.Computer) { }
    }

    // Registers the Computer cards next to the integration cards.
    public sealed class ComputerAutomationModule : IAutomationModule
    {
        public void RegisterTriggers(TriggerRegistry registry)
        {
            registry.Register<ComputerFileCreatedTrigger>(ComputerTypeCodes.FileCreated, IntegrationKinds.Computer);
            registry.Register<ComputerStatusTrigger>(ComputerTypeCodes.StatusChanged, IntegrationKinds.Computer);
            registry.Register<ComputerProgramStartedTrigger>(ComputerTypeCodes.ProgramStarted, IntegrationKinds.Computer);
            registry.Register<ComputerFileChangedTrigger>(ComputerTypeCodes.FileChanged, IntegrationKinds.Computer);
            registry.Register<ComputerFileDeletedTrigger>(ComputerTypeCodes.FileDeleted, IntegrationKinds.Computer);
            registry.Register<ComputerProgramStoppedTrigger>(ComputerTypeCodes.ProgramStopped, IntegrationKinds.Computer);
            registry.Register<ComputerSessionTrigger>(ComputerTypeCodes.SessionChanged, IntegrationKinds.Computer);
            registry.Register<ComputerClipboardTrigger>(ComputerTypeCodes.ClipboardChanged, IntegrationKinds.Computer);
            registry.Register<ComputerPowerTrigger>(ComputerTypeCodes.PowerChanged, IntegrationKinds.Computer);
            registry.Register<ComputerDriveTrigger>(ComputerTypeCodes.DriveChanged, IntegrationKinds.Computer);
            registry.Register<ComputerIdleTrigger>(ComputerTypeCodes.IdleChanged, IntegrationKinds.Computer);
            registry.Register<ComputerDiskLowTrigger>(ComputerTypeCodes.DiskLow, IntegrationKinds.Computer);
            registry.Register<ComputerGitNewCommitTrigger>(ComputerTypeCodes.GitNewCommit, IntegrationKinds.Computer);
            registry.Register<ComputerGitChangedTrigger>(ComputerTypeCodes.GitChanged, IntegrationKinds.Computer);
        }

        public void RegisterActions(ActionRegistry registry)
        {
            registry.Register<ComputerNotifyAction>(ComputerTypeCodes.Notify, IntegrationKinds.Computer);
            registry.Register<ComputerMessageBoxAction>(ComputerTypeCodes.MessageBox, IntegrationKinds.Computer);
            registry.Register<ComputerSpeakAction>(ComputerTypeCodes.Speak, IntegrationKinds.Computer);
            registry.Register<ComputerOpenAction>(ComputerTypeCodes.Open, IntegrationKinds.Computer);
            registry.Register<ComputerClipboardSetAction>(ComputerTypeCodes.ClipboardSet, IntegrationKinds.Computer);
            registry.Register<ComputerClipboardGetAction>(ComputerTypeCodes.ClipboardGet, IntegrationKinds.Computer);
            registry.Register<ComputerSystemInfoAction>(ComputerTypeCodes.SystemInfo, IntegrationKinds.Computer);
            registry.Register<ComputerScreenshotAction>(ComputerTypeCodes.Screenshot, IntegrationKinds.Computer);
            registry.Register<ComputerProcessRunningAction>(ComputerTypeCodes.ProcessRunning, IntegrationKinds.Computer);
            registry.Register<ComputerReadFileAction>(ComputerTypeCodes.ReadFile, IntegrationKinds.Computer);
            registry.Register<ComputerWriteFileAction>(ComputerTypeCodes.WriteFile, IntegrationKinds.Computer);
            registry.Register<ComputerListFolderAction>(ComputerTypeCodes.ListFolder, IntegrationKinds.Computer);
            registry.Register<ComputerCopyFileAction>(ComputerTypeCodes.CopyFile, IntegrationKinds.Computer);
            registry.Register<ComputerMoveFileAction>(ComputerTypeCodes.MoveFile, IntegrationKinds.Computer);
            registry.Register<ComputerZipFolderAction>(ComputerTypeCodes.ZipFolder, IntegrationKinds.Computer);
            registry.Register<ComputerSaveAttachmentAction>(ComputerTypeCodes.SaveAttachment, IntegrationKinds.Computer);
            registry.Register<ComputerGetFileAction>(ComputerTypeCodes.GetFile, IntegrationKinds.Computer);
            registry.Register<ComputerRunProgramAction>(ComputerTypeCodes.RunProgram, IntegrationKinds.Computer);
            registry.Register<ComputerRunScriptAction>(ComputerTypeCodes.RunScript, IntegrationKinds.Computer);
            registry.Register<ComputerCloseProgramAction>(ComputerTypeCodes.CloseProgram, IntegrationKinds.Computer);
            registry.Register<ComputerLockAction>(ComputerTypeCodes.Lock, IntegrationKinds.Computer);
            registry.Register<ComputerSleepAction>(ComputerTypeCodes.Sleep, IntegrationKinds.Computer);
            registry.Register<ComputerDisplayOffAction>(ComputerTypeCodes.DisplayOff, IntegrationKinds.Computer);
            registry.Register<ComputerShutdownAction>(ComputerTypeCodes.Shutdown, IntegrationKinds.Computer);
            registry.Register<ComputerCancelShutdownAction>(ComputerTypeCodes.CancelShutdown, IntegrationKinds.Computer);
            registry.Register<ComputerVolumeAction>(ComputerTypeCodes.Volume, IntegrationKinds.Computer);
            registry.Register<ComputerMediaAction>(ComputerTypeCodes.Media, IntegrationKinds.Computer);
            registry.Register<ComputerActiveWindowAction>(ComputerTypeCodes.ActiveWindow, IntegrationKinds.Computer);
            registry.Register<ComputerListProgramsAction>(ComputerTypeCodes.ListPrograms, IntegrationKinds.Computer);
            registry.Register<ComputerNetworkInfoAction>(ComputerTypeCodes.NetworkInfo, IntegrationKinds.Computer);
            registry.Register<ComputerPingHostAction>(ComputerTypeCodes.PingHost, IntegrationKinds.Computer);
            registry.Register<ComputerCreateFolderAction>(ComputerTypeCodes.CreateFolder, IntegrationKinds.Computer);
            registry.Register<ComputerDeleteFileAction>(ComputerTypeCodes.DeleteFile, IntegrationKinds.Computer);
            registry.Register<ComputerFileInfoAction>(ComputerTypeCodes.FileInfo, IntegrationKinds.Computer);
            registry.Register<ComputerUnzipAction>(ComputerTypeCodes.Unzip, IntegrationKinds.Computer);
            registry.Register<ComputerDownloadUrlAction>(ComputerTypeCodes.DownloadUrl, IntegrationKinds.Computer);
            registry.Register<ComputerGitStatusAction>(ComputerTypeCodes.GitStatus, IntegrationKinds.Computer);
            registry.Register<ComputerGitLogAction>(ComputerTypeCodes.GitLog, IntegrationKinds.Computer);
            registry.Register<ComputerGitDiffAction>(ComputerTypeCodes.GitDiff, IntegrationKinds.Computer);
            registry.Register<ComputerGitBranchesAction>(ComputerTypeCodes.GitBranches, IntegrationKinds.Computer);
            registry.Register<ComputerGitFetchAction>(ComputerTypeCodes.GitFetch, IntegrationKinds.Computer);
            registry.Register<ComputerGitPullAction>(ComputerTypeCodes.GitPull, IntegrationKinds.Computer);
            registry.Register<ComputerGitCommitAction>(ComputerTypeCodes.GitCommit, IntegrationKinds.Computer);
            registry.Register<ComputerGitPushAction>(ComputerTypeCodes.GitPush, IntegrationKinds.Computer);
            registry.Register<ComputerGitSwitchAction>(ComputerTypeCodes.GitSwitch, IntegrationKinds.Computer);
            registry.Register<ComputerGitCloneAction>(ComputerTypeCodes.GitClone, IntegrationKinds.Computer);
            registry.Register<ComputerGitInitAction>(ComputerTypeCodes.GitInit, IntegrationKinds.Computer);
            registry.Register<ComputerGitMergeAction>(ComputerTypeCodes.GitMerge, IntegrationKinds.Computer);
            registry.Register<ComputerGitTagAction>(ComputerTypeCodes.GitTag, IntegrationKinds.Computer);
            registry.Register<ComputerGitStashAction>(ComputerTypeCodes.GitStash, IntegrationKinds.Computer);
            registry.Register<ComputerGitCommandAction>(ComputerTypeCodes.GitCommand, IntegrationKinds.Computer);
        }
    }
}
