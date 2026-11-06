using Partnerly.Automation.Abstractions;

namespace Partnerly.Computer
{
    public static class ComputerServiceCollectionExtensions
    {
        // Call after AddPartnerlyIntegrations (the Computer cards use its connections).
        public static IServiceCollection AddPartnerlyComputer(this IServiceCollection services)
        {
            services.AddSingleton<ComputerGateway>();
            services.AddSingleton<IComputerGateway>(sp => sp.GetRequiredService<ComputerGateway>());
            services.AddSingleton<IAutomationModule, ComputerAutomationModule>();

            services.AddTransient<ComputerFileCreatedTrigger>();
            services.AddTransient<ComputerStatusTrigger>();
            services.AddTransient<ComputerProgramStartedTrigger>();
            services.AddTransient<ComputerFileChangedTrigger>();
            services.AddTransient<ComputerFileDeletedTrigger>();
            services.AddTransient<ComputerProgramStoppedTrigger>();
            services.AddTransient<ComputerSessionTrigger>();
            services.AddTransient<ComputerClipboardTrigger>();
            services.AddTransient<ComputerPowerTrigger>();
            services.AddTransient<ComputerDriveTrigger>();
            services.AddTransient<ComputerIdleTrigger>();
            services.AddTransient<ComputerDiskLowTrigger>();
            services.AddTransient<ComputerDisplayOffAction>();
            services.AddTransient<ComputerShutdownAction>();
            services.AddTransient<ComputerCancelShutdownAction>();
            services.AddTransient<ComputerVolumeAction>();
            services.AddTransient<ComputerMediaAction>();
            services.AddTransient<ComputerActiveWindowAction>();
            services.AddTransient<ComputerListProgramsAction>();
            services.AddTransient<ComputerNetworkInfoAction>();
            services.AddTransient<ComputerPingHostAction>();
            services.AddTransient<ComputerCreateFolderAction>();
            services.AddTransient<ComputerDeleteFileAction>();
            services.AddTransient<ComputerFileInfoAction>();
            services.AddTransient<ComputerUnzipAction>();
            services.AddTransient<ComputerDownloadUrlAction>();

            services.AddTransient<ComputerNotifyAction>();
            services.AddTransient<ComputerMessageBoxAction>();
            services.AddTransient<ComputerSpeakAction>();
            services.AddTransient<ComputerOpenAction>();
            services.AddTransient<ComputerClipboardSetAction>();
            services.AddTransient<ComputerClipboardGetAction>();
            services.AddTransient<ComputerSystemInfoAction>();
            services.AddTransient<ComputerScreenshotAction>();
            services.AddTransient<ComputerProcessRunningAction>();
            services.AddTransient<ComputerReadFileAction>();
            services.AddTransient<ComputerWriteFileAction>();
            services.AddTransient<ComputerListFolderAction>();
            services.AddTransient<ComputerCopyFileAction>();
            services.AddTransient<ComputerMoveFileAction>();
            services.AddTransient<ComputerZipFolderAction>();
            services.AddTransient<ComputerSaveAttachmentAction>();
            services.AddTransient<ComputerGetFileAction>();
            services.AddTransient<ComputerRunProgramAction>();
            services.AddTransient<ComputerRunScriptAction>();
            services.AddTransient<ComputerCloseProgramAction>();
            services.AddTransient<ComputerLockAction>();
            services.AddTransient<ComputerSleepAction>();
            return services;
        }
    }
}
