namespace Partnerly.Computer
{
    // Type codes of the "Computer" cards. Actions are run by the Partnerly Agent
    // program on the user's PC; triggers are events that program reports.
    public static class ComputerTypeCodes
    {
        public const string Notify = "computer.notify";
        public const string MessageBox = "computer.message_box";
        public const string Open = "computer.open";
        public const string ClipboardSet = "computer.clipboard_set";
        public const string ClipboardGet = "computer.clipboard_get";
        public const string Screenshot = "computer.screenshot";
        public const string SystemInfo = "computer.system_info";
        public const string ReadFile = "computer.read_file";
        public const string WriteFile = "computer.write_file";
        public const string ListFolder = "computer.list_folder";
        public const string CopyFile = "computer.copy_file";
        public const string MoveFile = "computer.move_file";
        public const string ZipFolder = "computer.zip_folder";
        public const string SaveAttachment = "computer.save_attachment";
        public const string GetFile = "computer.get_file";
        public const string RunProgram = "computer.run_program";
        public const string RunScript = "computer.run_script";
        public const string ProcessRunning = "computer.process_running";
        public const string CloseProgram = "computer.close_program";
        public const string Lock = "computer.lock";
        public const string Sleep = "computer.sleep";
        public const string Speak = "computer.speak";

        public const string CreateFolder = "computer.create_folder";
        public const string DeleteFile = "computer.delete_file";
        public const string FileInfo = "computer.file_info";
        public const string Unzip = "computer.unzip";
        public const string ListPrograms = "computer.list_programs";
        public const string Volume = "computer.volume";
        public const string Media = "computer.media";
        public const string DisplayOff = "computer.display_off";
        public const string Shutdown = "computer.shutdown";
        public const string CancelShutdown = "computer.cancel_shutdown";
        public const string ActiveWindow = "computer.active_window";
        public const string NetworkInfo = "computer.network_info";
        public const string PingHost = "computer.ping_host";
        public const string DownloadUrl = "computer.download_url";

        public const string GitStatus = "computer.git_status";
        public const string GitLog = "computer.git_log";
        public const string GitDiff = "computer.git_diff";
        public const string GitBranches = "computer.git_branches";
        public const string GitFetch = "computer.git_fetch";
        public const string GitPull = "computer.git_pull";
        public const string GitCommit = "computer.git_commit";
        public const string GitPush = "computer.git_push";
        public const string GitSwitch = "computer.git_switch";
        public const string GitClone = "computer.git_clone";
        public const string GitInit = "computer.git_init";
        public const string GitMerge = "computer.git_merge";
        public const string GitTag = "computer.git_tag";
        public const string GitStash = "computer.git_stash";
        public const string GitCommand = "computer.git_command";
        public const string GitNewCommit = "computer.git_new_commit";
        public const string GitChanged = "computer.git_changed";

        public const string FileChanged = "computer.file_changed";
        public const string FileDeleted = "computer.file_deleted";
        public const string ProgramStopped = "computer.program_stopped";
        public const string SessionChanged = "computer.session_changed";
        public const string ClipboardChanged = "computer.clipboard_changed";
        public const string PowerChanged = "computer.power_changed";
        public const string DriveChanged = "computer.drive_changed";
        public const string IdleChanged = "computer.idle_changed";
        public const string DiskLow = "computer.disk_low";

        public const string FileCreated = "computer.file_created";
        public const string StatusChanged = "computer.status_changed";
        public const string ProgramStarted = "computer.program_started";
    }
}
