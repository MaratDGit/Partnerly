// Property forms of the "Computer" cards (Partnerly Agent on the user's PC).
// Every card starts with one plain sentence about what it does; paths are a dropdown of the folders
// the chosen PC allows (the program on the PC reports them) that still accepts a typed value.
(function () {
    "use strict";

    window.partnerlyComputerForms = function (t, h) {
        var connection = h.connection, text = h.text, area = h.area, number = h.number, yesNo = h.yesNo, note = h.note;

        function intro(name) { return note("intPc" + name + "Intro"); }

        // pick: what the "Choose..." button opens on the PC (file, folder, save, program, programname, script, zip, zipsave;
        // an array shows one button per kind). Typing the value by hand still works.
        function pcText(key, labelKey, fallback, helpKey, source, pick) {
            var field = text(key, labelKey, fallback, helpKey);
            field.source = source || "computerFolders";
            if (pick) { field.pick = pick; }
            return field;
        }

        function choice(key, labelKey, fallback, options) {
            return {
                key: key,
                type: "select",
                label: t(labelKey, fallback),
                options: options.map(function (item) { return { value: item[0], label: t(item[1], item[2]) }; })
            };
        }

        // Git cards: the repository folder is one of the folders the PC allows for Git.
        function repo() { return pcText("repo", "intPcGitRepo", "Repository folder", "intPcGitRepoHelp", "computerRepos", "repo"); }
        function gitPath() { return text("path", "intPcGitPath", "Only this file or folder", null, true); }
        function branch() { return text("branch", "intGhBranch", "Branch", "intGhBranchHelp", true); }

        var pc = connection("computer");
        var ask = function () { return note("intPcAskNote"); };

        return {
            // ---------- Git ----------
            "computer.git_status": [pc, intro("GitStatus"), repo()],
            "computer.git_log": [
                pc, intro("GitLog"), repo(),
                number("max", "intMaxResults", "Max results"),
                branch(), gitPath()
            ],
            "computer.git_diff": [
                pc, intro("GitDiff"), repo(),
                choice("mode", "intPcGitMode", "What to show", [["stat", "intPcGitStat", "Summary of changes"], ["names", "intPcGitNames", "Names of changed files"], ["full", "intPcGitFull", "Full text of changes"]]),
                yesNo("staged", "intPcGitStaged", "Only staged changes"),
                text("target", "intPcGitTarget", "Compare with (branch or commit)", null, true),
                gitPath()
            ],
            "computer.git_branches": [pc, intro("GitBranches"), repo(), yesNo("remote", "intPcGitIncludeRemote", "Include branches from the server")],
            "computer.git_fetch": [
                pc, intro("GitFetch"), repo(),
                text("remote", "intPcGitRemote", "Remote", "intPcGitRemoteHelp", true),
                yesNo("prune", "intPcGitPrune", "Forget branches deleted on the server")
            ],
            "computer.git_pull": [
                pc, intro("GitPull"), repo(),
                text("remote", "intPcGitRemote", "Remote", "intPcGitRemoteHelp", true),
                branch(),
                choice("mode", "intPcGitPullMode", "How to join", [["ff", "intPcGitFfOnly", "Only fast-forward (safest)"], ["merge", "intPcGitMergeMode", "Merge"], ["rebase", "intPcGitRebaseMode", "Rebase (asks you on the PC)"]])
            ],
            "computer.git_commit": [
                pc, intro("GitCommit"), repo(),
                area("message", "intPcGitMessage", "Message", "intVariablesHelp"),
                choice("add", "intPcGitAdd", "What to commit", [["all", "intPcGitAddAll", "Everything: new, changed and deleted files"], ["tracked", "intPcGitAddTracked", "Only files git already knows"], ["paths", "intPcGitAddPaths", "Only the files listed below"], ["none", "intPcGitAddNone", "Only what is already staged"]]),
                area("paths", "intPcGitPaths", "Files (one per line)", "intPcGitPathsHelp")
            ],
            "computer.git_push": [
                pc, intro("GitPush"), repo(),
                text("remote", "intPcGitRemote", "Remote", "intPcGitRemoteHelp", true),
                branch(),
                yesNo("setUpstream", "intPcGitSetUpstream", "Remember this branch on the server (-u)"),
                yesNo("tags", "intPcGitTags", "Send tags too"),
                choice("force", "intPcGitForce", "Overwrite the branch on the server", [["none", "intPcGitForceNone", "No"], ["lease", "intPcGitForceLease", "Yes, but only if nobody else pushed (safer)"], ["force", "intPcGitForceYes", "Yes, force it"]]),
                note("intPcGitPushNote")
            ],
            "computer.git_switch": [
                pc, intro("GitSwitch"), repo(),
                branch(),
                yesNo("create", "intPcGitCreate", "Create the branch if it does not exist"),
                text("from", "intPcGitFrom", "Start from (branch or commit)", null, true)
            ],
            "computer.git_clone": [
                pc, intro("GitClone"),
                text("url", "intPcGitUrl", "Repository address", "intPcGitUrlHelp", true),
                pcText("repo", "intPcGitCloneFolder", "Folder for the copy", "intPcGitCloneFolderHelp", "computerFolders", "folder"),
                branch(),
                number("depth", "intPcGitDepth", "Only the last N commits (0 = all)")
            ],
            "computer.git_init": [pc, intro("GitInit"), pcText("repo", "intPcGitRepo", "Repository folder", "intPcGitCloneFolderHelp", "computerFolders", "folder"), branch()],
            "computer.git_merge": [
                pc, intro("GitMerge"), repo(),
                text("branch", "intGhBranch", "Branch", null, true),
                yesNo("noFastForward", "intPcGitNoFf", "Always make a merge commit"),
                text("message", "intPcGitMessage", "Message")
            ],
            "computer.git_tag": [
                pc, intro("GitTag"), repo(),
                text("name", "intPcGitTagName", "Tag name", null, true),
                text("message", "intPcGitTagMessage", "Note for the tag (optional)"),
                text("target", "intPcGitTarget", "Compare with (branch or commit)", null, true),
                yesNo("delete", "intPcGitDelete", "Delete this tag")
            ],
            "computer.git_stash": [
                pc, intro("GitStash"), repo(),
                choice("action", "intPcGitStashAction", "What to do", [["push", "intPcGitStashPush", "Put the changes aside"], ["pop", "intPcGitStashPop", "Bring them back and remove from the shelf"], ["apply", "intPcGitStashApply", "Bring them back and keep on the shelf"], ["list", "intPcGitStashList", "Show what is on the shelf"], ["drop", "intPcGitStashDrop", "Throw the last one away (asks you on the PC)"]]),
                text("message", "intPcGitMessage", "Message")
            ],
            "computer.git_command": [
                pc, intro("GitCommand"), repo(),
                text("command", "intPcGitCommand", "Git command", "intPcGitCommandHelp", true),
                note("intPcGitCommandNote")
            ],
            "computer.git_new_commit": [
                pc, intro("GitNewCommit"),
                pcText("repo", "intPcGitRepo", "Repository folder", "intPcGitTriggerRepoHelp", "computerRepos", "repo"),
                branch()
            ],
            "computer.git_changed": [
                pc, intro("GitChanged"),
                pcText("repo", "intPcGitRepo", "Repository folder", "intPcGitTriggerRepoHelp", "computerRepos", "repo"),
                branch(),
                choice("state", "intPcGitState", "When", [["any", "intPcGitStateAny", "Gets changes or becomes clean"], ["dirty", "intPcGitStateDirty", "Gets uncommitted changes"], ["clean", "intPcGitStateClean", "Becomes clean again"]])
            ],

            // ---------- Show something on the PC ----------
            "computer.notify": [
                pc, intro("Notify"),
                text("title", "intPcTitle", "Title"),
                area("text", "intPcText", "Text", "intVariablesHelp")
            ],
            "computer.message_box": [
                pc, intro("MessageBox"),
                text("title", "intPcTitle", "Title"),
                area("text", "intPcText", "Text", "intVariablesHelp"),
                choice("buttons", "intPcButtons", "Buttons", [["ok", "intPcOk", "OK"], ["yesno", "intPcYesNo", "Yes / No"]]),
                note("intPcMessageBoxNote")
            ],
            "computer.speak": [pc, intro("Speak"), area("text", "intPcText", "Text", "intVariablesHelp")],
            "computer.open": [pc, intro("Open"), pcText("target", "intPcTarget", "Link, file or folder", "intPcTargetHelp", null, ["file", "folder"])],

            // ---------- Clipboard, information ----------
            "computer.clipboard_set": [pc, intro("ClipboardSet"), area("text", "intPcText", "Text", "intVariablesHelp")],
            "computer.clipboard_get": [pc, intro("ClipboardGet")],
            "computer.system_info": [pc, intro("SystemInfo")],
            "computer.screenshot": [
                pc, intro("Screenshot"),
                choice("screen", "intPcScreen", "Screen", [["main", "intPcScreenMain", "Main screen"], ["all", "intPcScreenAll", "All screens"]])
            ],
            "computer.active_window": [pc, intro("ActiveWindow")],
            "computer.list_programs": [pc, intro("ListPrograms")],
            "computer.network_info": [pc, intro("NetworkInfo")],
            "computer.ping_host": [pc, intro("PingHost"), text("host", "intPcHost", "Site or device", "intPcHostHelp", true)],
            "computer.process_running": [pc, intro("ProcessRunning"), pcText("name", "intPcProgramName", "Program name", "intPcProgramNameHelp", "computerPrograms", "programname")],

            // ---------- Files ----------
            "computer.read_file": [
                pc, intro("ReadFile"),
                pcText("path", "intPcFile", "File", "intPcPathHelp", null, "file"),
                number("maxKb", "intPcMaxKb", "Largest size (KB)")
            ],
            "computer.write_file": [
                pc, intro("WriteFile"),
                pcText("path", "intPcFile", "File", "intPcPathHelp", null, "save"),
                area("text", "intPcText", "Text", "intVariablesHelp"),
                choice("mode", "intPcMode", "If the file exists", [["overwrite", "intPcReplace", "Replace it"], ["append", "intPcAppend", "Add to the end"]])
            ],
            "computer.file_info": [pc, intro("FileInfo"), pcText("path", "intPcFileOrFolder", "File or folder", "intPcPathHelp", null, ["file", "folder"])],
            "computer.list_folder": [
                pc, intro("ListFolder"),
                pcText("path", "intPcFolder", "Folder", "intPcPathHelp", null, "folder"),
                text("pattern", "intPcPattern", "Mask", "intPcPatternHelp", true),
                yesNo("recursive", "intPcRecursive", "Include subfolders")
            ],
            "computer.create_folder": [pc, intro("CreateFolder"), pcText("path", "intPcFolder", "Folder", "intPcPathHelp", null, "folder")],
            "computer.copy_file": [
                pc, intro("CopyFile"),
                pcText("source", "intPcSource", "From", "intPcPathHelp", null, ["file", "folder"]),
                pcText("destination", "intPcDestination", "To", "intPcPathHelp", null, ["folder", "save"]),
                yesNo("overwrite", "intPcOverwrite", "Replace if it exists")
            ],
            "computer.move_file": [
                pc, intro("MoveFile"),
                pcText("source", "intPcSource", "From", "intPcPathHelp", null, ["file", "folder"]),
                pcText("destination", "intPcDestination", "To", "intPcPathHelp", null, ["folder", "save"]),
                yesNo("overwrite", "intPcOverwrite", "Replace if it exists")
            ],
            "computer.delete_file": [
                pc, intro("DeleteFile"),
                pcText("path", "intPcFile", "File", "intPcPathHelp", null, "file"),
                note("intPcDeleteNote")
            ],
            "computer.zip_folder": [
                pc, intro("ZipFolder"),
                pcText("source", "intPcFolder", "Folder", "intPcPathHelp", null, "folder"),
                pcText("destination", "intPcZipFile", "Archive file (.zip)", "intPcPathHelp", null, "zipsave")
            ],
            "computer.unzip": [
                pc, intro("Unzip"),
                pcText("source", "intPcZipFile", "Archive file (.zip)", "intPcPathHelp", null, "zip"),
                pcText("destination", "intPcUnzipTo", "Unpack into folder", "intPcPathHelp", null, "folder"),
                yesNo("overwrite", "intPcOverwrite", "Replace if it exists")
            ],
            "computer.save_attachment": [
                pc, intro("SaveAttachment"),
                text("attachmentId", "intAttachment", "Attachment id", "intAttachmentHelp"),
                pcText("folder", "intPcFolder", "Folder", "intPcPathHelp", null, "folder"),
                text("fileName", "intFileName", "File name", "intPcSaveNameHelp"),
                yesNo("overwrite", "intPcOverwrite", "Replace if it exists")
            ],
            "computer.get_file": [
                pc, intro("GetFile"),
                pcText("path", "intPcFile", "File", "intPcPathHelp", null, "file")
            ],
            "computer.download_url": [
                pc, intro("DownloadUrl"),
                text("url", "intPcUrl", "Link to the file", "intPcUrlHelp"),
                pcText("folder", "intPcFolder", "Folder", "intPcPathHelp", null, "folder"),
                text("fileName", "intFileName", "File name", "intPcDownloadNameHelp"),
                yesNo("overwrite", "intPcOverwrite", "Replace if it exists")
            ],

            // ---------- Programs ----------
            "computer.run_program": [
                pc, intro("RunProgram"),
                pcText("path", "intPcProgram", "Program", "intPcProgramHelp", "computerPrograms", "program"),
                text("arguments", "intPcArguments", "Arguments", "intPcArgumentsHelp"),
                yesNo("wait", "intPcWait", "Wait for it to finish"),
                number("timeoutSeconds", "intPcTimeout", "Stop after (seconds)"),
                ask()
            ],
            "computer.run_script": [
                pc, intro("RunScript"),
                pcText("path", "intPcScript", "Script (.ps1, .bat, .cmd)", "intPcScriptHelp", "computerScripts", "script"),
                text("arguments", "intPcArguments", "Arguments", "intPcArgumentsHelp"),
                yesNo("wait", "intPcWait", "Wait for it to finish"),
                number("timeoutSeconds", "intPcTimeout", "Stop after (seconds)"),
                ask()
            ],
            "computer.close_program": [
                pc, intro("CloseProgram"),
                pcText("name", "intPcProgramName", "Program name", "intPcCloseHelp", "computerPrograms", "programname")
            ],

            // ---------- The computer ----------
            "computer.lock": [pc, intro("Lock")],
            "computer.sleep": [pc, intro("Sleep")],
            "computer.display_off": [pc, intro("DisplayOff")],
            "computer.shutdown": [
                pc, intro("Shutdown"),
                choice("mode", "intPcShutdownMode", "What to do", [
                    ["shutdown", "intPcModeShutdown", "Shut down"],
                    ["restart", "intPcModeRestart", "Restart"],
                    ["logoff", "intPcModeLogoff", "Sign out (no waiting)"]
                ]),
                number("delaySeconds", "intPcDelaySeconds", "Wait before (seconds)"),
                note("intPcShutdownNote"),
                ask()
            ],
            "computer.cancel_shutdown": [pc, intro("CancelShutdown")],
            "computer.volume": [
                pc, intro("Volume"),
                choice("action", "intPcVolumeAction", "What to do", [
                    ["up", "intPcVolumeUp", "Louder"],
                    ["down", "intPcVolumeDown", "Quieter"],
                    ["mute", "intPcVolumeMute", "Mute / unmute"]
                ]),
                number("steps", "intPcVolumeSteps", "How many steps"),
                note("intPcVolumeStepsHelp")
            ],
            "computer.media": [
                pc, intro("Media"),
                choice("action", "intPcMediaAction", "Button", [
                    ["playpause", "intPcMediaPlayPause", "Play / pause"],
                    ["next", "intPcMediaNext", "Next track"],
                    ["previous", "intPcMediaPrevious", "Previous track"],
                    ["stop", "intPcMediaStop", "Stop"]
                ])
            ],

            // ---------- Triggers: what happens on the PC ----------
            "computer.file_created": [
                pc, intro("FileCreated"),
                pcText("folder", "intPcWatchFolder", "Only this folder", "intPcWatchFolderHelp", null, "folder"),
                text("pattern", "intPcPattern", "Mask", "intPcPatternHelp", true),
                note("intPcFileTriggerHelp")
            ],
            "computer.file_changed": [
                pc, intro("FileChanged"),
                pcText("folder", "intPcWatchFolder", "Only this folder", "intPcWatchFolderHelp", null, "folder"),
                text("pattern", "intPcPattern", "Mask", "intPcPatternHelp", true),
                note("intPcFileTriggerHelp")
            ],
            "computer.file_deleted": [
                pc, intro("FileDeleted"),
                pcText("folder", "intPcWatchFolder", "Only this folder", "intPcWatchFolderHelp", null, "folder"),
                text("pattern", "intPcPattern", "Mask", "intPcPatternHelp", true),
                note("intPcFileTriggerHelp")
            ],
            "computer.program_started": [
                pc, intro("ProgramStarted"),
                pcText("program", "intPcProgramName", "Program name", "intPcProgramTriggerHelp", "computerPrograms", "programname"),
                note("intPcProgramWatchHelp")
            ],
            "computer.program_stopped": [
                pc, intro("ProgramStopped"),
                pcText("program", "intPcProgramName", "Program name", "intPcProgramTriggerHelp", "computerPrograms", "programname"),
                note("intPcProgramWatchHelp")
            ],
            "computer.status_changed": [
                pc, intro("StatusChanged"),
                choice("state", "intPcState", "When the PC", [
                    ["any", "intPcStateAny", "Goes online or offline"],
                    ["online", "intPcStateOnline", "Goes online"],
                    ["offline", "intPcStateOffline", "Goes offline"]
                ])
            ],
            "computer.session_changed": [
                pc, intro("SessionChanged"),
                choice("state", "intPcWhat", "What happened", [
                    ["any", "intPcAny", "Any of these"],
                    ["locked", "intPcSessionLocked", "Screen locked"],
                    ["unlocked", "intPcSessionUnlocked", "Screen unlocked"],
                    ["woke_up", "intPcSessionWoke", "PC woke up from sleep"],
                    ["going_to_sleep", "intPcSessionSleeping", "PC is going to sleep"],
                    ["signed_in", "intPcSessionSignedIn", "Someone signed in to Windows"],
                    ["signed_out", "intPcSessionSignedOut", "Someone signed out of Windows"]
                ])
            ],
            "computer.clipboard_changed": [
                pc, intro("ClipboardChanged"),
                text("textContains", "intTextContains", "Text contains"),
                note("intPcClipboardTriggerNote")
            ],
            "computer.power_changed": [
                pc, intro("PowerChanged"),
                choice("state", "intPcWhat", "What happened", [
                    ["any", "intPcAny", "Any of these"],
                    ["unplugged", "intPcPowerUnplugged", "Charger unplugged"],
                    ["plugged_in", "intPcPowerPluggedIn", "Charger plugged in"],
                    ["battery_low", "intPcPowerLow", "Battery is low (20%, 10%, 5%)"]
                ])
            ],
            "computer.drive_changed": [
                pc, intro("DriveChanged"),
                choice("state", "intPcWhat", "What happened", [
                    ["any", "intPcAny", "Any of these"],
                    ["connected", "intPcDriveConnected", "Drive plugged in"],
                    ["removed", "intPcDriveRemoved", "Drive removed"]
                ]),
                text("drive", "intPcDrive", "Only this drive letter", "intPcDriveHelp", true)
            ],
            "computer.idle_changed": [
                pc, intro("IdleChanged"),
                choice("state", "intPcWhat", "What happened", [
                    ["any", "intPcAny", "Any of these"],
                    ["idle", "intPcIdleStarted", "Nobody has touched the PC for a while"],
                    ["active", "intPcIdleEnded", "Somebody is back"]
                ]),
                note("intPcIdleNote")
            ],
            "computer.disk_low": [
                pc, intro("DiskLow"),
                text("drive", "intPcDrive", "Only this drive letter", "intPcDriveHelp", true),
                note("intPcDiskLowNote")
            ]
        };
    };
})();
