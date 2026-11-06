using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Agent.Config;
using Partnerly.Agent.Security;
using Partnerly.Agent.UI;

namespace Partnerly.Agent.Commands
{
    // Runs the commands the site sends. Every command is checked against the settings of
    // this PC first (AgentConfig); anything outside the lists is refused with a clear reason.
    public sealed class CommandRunner
    {
        private const long MaxFileBytes = 8L * 1024 * 1024;
        private const int MaxListEntries = 200;
        private const int MaxOutputChars = 8000;

        private static readonly string[] ScriptExtensions = { ".ps1", ".bat", ".cmd" };

        // Things the "open" command refuses to launch: programs go through run_program.
        private static readonly string[] ProgramExtensions =
        {
            ".exe", ".com", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh",
            ".msi", ".msp", ".scr", ".lnk", ".reg", ".hta", ".cpl", ".jar", ".dll", ".sys"
        };

        private readonly AgentConfig _config;
        private readonly UiBridge _ui;
        private readonly PathGuard _folders;
        private readonly PathGuard _scriptFolders;

        public CommandRunner(AgentConfig config, UiBridge ui)
        {
            _config = config;
            _ui = ui;
            _folders = new PathGuard(config.AllowedFolders);
            _scriptFolders = new PathGuard(config.ScriptFolders);
        }

        public async Task<JsonObject> RunAsync(string op, JsonObject args, CancellationToken cancellationToken)
        {
            if (_config.ConfirmOps.Contains(op, StringComparer.OrdinalIgnoreCase)
                && !_ui.Confirm($"Partnerly wants to run \"{op}\" on this PC.\n\n{Describe(args)}\n\nAllow?"))
            {
                throw new AgentException("The person at this PC declined the command.");
            }

            switch (op)
            {
                case "ping": return new JsonObject { ["machine"] = Environment.MachineName };
                case "notify": return Notify(args);
                case "message_box": return MessageBox(args);
                case "speak": return await SpeakAsync(args, cancellationToken);
                case "open": return Open(args);
                case "clipboard_set": _ui.SetClipboard(Text(args, "text")); return new JsonObject { ["done"] = true };
                case "clipboard_get": return new JsonObject { ["text"] = _ui.GetClipboard() };
                case "system_info": return SystemInfo();
                case "screenshot": return Screenshot(args);
                case "process_running": return ProcessRunning(args);
                case "read_file": return ReadFile(args);
                case "write_file": return WriteFile(args);
                case "list_folder": return ListFolder(args);
                case "copy_file": return CopyOrMove(args, move: false);
                case "move_file": return CopyOrMove(args, move: true);
                case "zip_folder": return ZipFolder(args);
                case "save_file": return SaveFile(args);
                case "get_file": return GetFile(args);
                case "run_program": return await RunProgramAsync(args, script: false, cancellationToken);
                case "run_script": return await RunProgramAsync(args, script: true, cancellationToken);
                case "close_program": return CloseProgram(args);
                case "lock": return Lock();
                case "sleep": return Sleep();
                case "create_folder": return CreateFolder(args);
                case "delete_file": return DeleteFile(args);
                case "file_info": return FileInformation(args);
                case "unzip": return Unzip(args);
                case "list_programs": return ListPrograms();
                case "volume": return Volume(args);
                case "media": return Media(args);
                case "display_off": NativeMethods.MonitorOff(); return new JsonObject { ["done"] = true };
                case "shutdown": return Shutdown(args);
                case "cancel_shutdown": return CancelShutdown();
                case "active_window": return ActiveWindow();
                case "network_info": return NetworkInformationResult();
                case "ping_host": return await PingAsync(args);
                case "download_url": return await DownloadUrlAsync(args, cancellationToken);
                default: throw new AgentException("This version of Partnerly Agent does not know the command \"" + op + "\". Update the program.");
            }
        }

        // ---------- screen ----------
        private JsonObject Notify(JsonObject args)
        {
            _ui.Notify(Text(args, "title"), Text(args, "text"));
            return new JsonObject { ["shown"] = true };
        }

        private JsonObject MessageBox(JsonObject args)
        {
            var yesNo = string.Equals(Text(args, "buttons"), "yesno", StringComparison.OrdinalIgnoreCase);
            var button = _ui.MessageBox(Text(args, "title"), Text(args, "text"), yesNo);
            return new JsonObject { ["button"] = button, ["confirmed"] = button is "OK" or "Yes" };
        }

        private async Task<JsonObject> SpeakAsync(JsonObject args, CancellationToken cancellationToken)
        {
            var text = Text(args, "text");
            if (text.Length == 0) { throw new AgentException("There is nothing to say."); }

            // The text goes in through standard input, so it can never become part of a command.
            var info = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -Command \"Add-Type -AssemblyName System.Speech; $t=[Console]::In.ReadToEnd(); (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak($t)\"")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(info) ?? throw new AgentException("Speech could not start.");
            await process.StandardInput.WriteAsync(text.Length > 2000 ? text[..2000] : text);
            process.StandardInput.Close();
            await process.WaitForExitAsync(cancellationToken);
            return new JsonObject { ["spoken"] = true };
        }

        // ---------- open / clipboard / info ----------
        private JsonObject Open(JsonObject args)
        {
            var target = Text(args, "target");
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                return new JsonObject { ["opened"] = uri.AbsoluteUri };
            }

            var path = _folders.Resolve(target);
            if (File.Exists(path))
            {
                if (ProgramExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                {
                    throw new AgentException("Programs and scripts are not opened this way. Use the Run program card.");
                }
            }
            else if (!Directory.Exists(path))
            {
                throw new AgentException("The file or folder was not found.");
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return new JsonObject { ["opened"] = path };
        }

        private JsonObject SystemInfo()
        {
            var drives = new JsonArray();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                drives.Add(new JsonObject
                {
                    ["name"] = drive.Name,
                    ["totalGb"] = Math.Round(drive.TotalSize / 1073741824.0, 1),
                    ["freeGb"] = Math.Round(drive.AvailableFreeSpace / 1073741824.0, 1)
                });
            }

            var power = SystemInformation.PowerStatus;
            var result = new JsonObject
            {
                ["machine"] = Environment.MachineName,
                ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ["cpuCount"] = Environment.ProcessorCount,
                ["memoryGb"] = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1073741824.0, 1),
                ["uptimeMinutes"] = (long)(TimeSpan.FromMilliseconds(Environment.TickCount64).TotalMinutes),
                ["drives"] = drives,
                ["onBattery"] = power.PowerLineStatus == PowerLineStatus.Offline,
                ["batteryPercent"] = power.BatteryLifePercent is >= 0f and <= 1f ? (int)Math.Round(power.BatteryLifePercent * 100) : null
            };
            return result;
        }

        private JsonObject Screenshot(JsonObject args)
        {
            var all = string.Equals(Text(args, "screen"), "all", StringComparison.OrdinalIgnoreCase);
            var bytes = _ui.Screenshot(all);
            if (bytes.Length > MaxFileBytes)
            {
                throw new AgentException("The screenshot is bigger than 8 MB. Try the main screen only.");
            }

            return new JsonObject
            {
                ["fileName"] = $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png",
                ["contentBase64"] = Convert.ToBase64String(bytes)
            };
        }

        private static JsonObject ProcessRunning(JsonObject args)
        {
            var name = StripExe(Text(args, "name"));
            if (name.Length == 0) { throw new AgentException("Program name is empty."); }
            var found = Process.GetProcessesByName(name);
            var count = found.Length;
            foreach (var process in found) { process.Dispose(); }
            return new JsonObject { ["running"] = count > 0, ["count"] = count };
        }

        // ---------- files ----------
        private JsonObject ReadFile(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            if (!File.Exists(path)) { throw new AgentException("The file was not found."); }
            var maxKb = int.TryParse(Text(args, "maxKb"), out var parsed) && parsed > 0 ? Math.Min(parsed, 1024) : 256;
            var info = new FileInfo(path);
            if (info.Length > maxKb * 1024L)
            {
                throw new AgentException($"The file is bigger than {maxKb} KB. Raise the limit (up to 1024) or use Get file from PC.");
            }

            return new JsonObject { ["text"] = File.ReadAllText(path), ["size"] = info.Length, ["fileName"] = info.Name };
        }

        private JsonObject WriteFile(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            var append = string.Equals(Text(args, "mode"), "append", StringComparison.OrdinalIgnoreCase);
            var text = args["text"]?.ToString() ?? string.Empty;
            if (Encoding.UTF8.GetByteCount(text) > MaxFileBytes) { throw new AgentException("The text is bigger than 8 MB."); }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (append) { File.AppendAllText(path, text); } else { File.WriteAllText(path, text); }
            return new JsonObject { ["path"] = path, ["size"] = new FileInfo(path).Length };
        }

        private JsonObject ListFolder(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            if (!Directory.Exists(path)) { throw new AgentException("The folder was not found."); }
            var pattern = Text(args, "pattern");
            var recursive = IsTrue(args, "recursive");
            var items = new JsonArray();
            var truncated = false;
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos(pattern.Length == 0 ? "*" : pattern,
                         recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) { continue; }
                if (items.Count >= MaxListEntries) { truncated = true; break; }
                items.Add(new JsonObject
                {
                    ["name"] = entry.Name,
                    ["path"] = entry.FullName,
                    ["isFolder"] = entry is DirectoryInfo,
                    ["size"] = entry is FileInfo file ? file.Length : null,
                    ["modified"] = entry.LastWriteTimeUtc.ToString("O")
                });
            }

            return new JsonObject { ["count"] = items.Count, ["truncated"] = truncated, ["items"] = items };
        }

        private JsonObject CopyOrMove(JsonObject args, bool move)
        {
            var source = _folders.Resolve(Text(args, "source"));
            var destination = _folders.Resolve(Text(args, "destination"));
            if (!File.Exists(source)) { throw new AgentException("The source file was not found."); }
            if (Directory.Exists(destination)) { destination = Path.Combine(destination, Path.GetFileName(source)); _folders.Resolve(destination); }
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var overwrite = IsTrue(args, "overwrite");
            if (move) { File.Move(source, destination, overwrite); } else { File.Copy(source, destination, overwrite); }
            return new JsonObject { ["path"] = destination };
        }

        private JsonObject ZipFolder(JsonObject args)
        {
            var source = _folders.Resolve(Text(args, "source"));
            var destination = _folders.Resolve(Text(args, "destination"));
            if (!Directory.Exists(source)) { throw new AgentException("The folder was not found."); }
            if (PathGuard.IsUnder(destination, source)) { throw new AgentException("The archive cannot be inside the folder it packs."); }
            if (File.Exists(destination)) { throw new AgentException("The archive already exists."); }
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            ZipFile.CreateFromDirectory(source, destination, CompressionLevel.Optimal, includeBaseDirectory: false);
            return new JsonObject { ["path"] = destination, ["size"] = new FileInfo(destination).Length };
        }

        private JsonObject SaveFile(JsonObject args)
        {
            var folder = _folders.Resolve(Text(args, "folder"));
            var fileName = Path.GetFileName(Text(args, "fileName"));
            if (fileName.Length == 0 || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { throw new AgentException("The file name is not valid."); }
            var content = Text(args, "contentBase64");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(content); }
            catch (FormatException) { throw new AgentException("The file came damaged."); }
            if (bytes.LongLength > MaxFileBytes) { throw new AgentException("The file is bigger than 8 MB."); }
            var path = _folders.Resolve(Path.Combine(folder, fileName));
            if (File.Exists(path) && !IsTrue(args, "overwrite")) { throw new AgentException("The file already exists. Turn on \"Replace if it exists\" to overwrite."); }
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(path, bytes);
            return new JsonObject { ["path"] = path, ["size"] = bytes.Length };
        }

        private JsonObject GetFile(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            if (!File.Exists(path)) { throw new AgentException("The file was not found."); }
            var info = new FileInfo(path);
            if (info.Length > MaxFileBytes) { throw new AgentException("The file is bigger than 8 MB."); }
            return new JsonObject { ["fileName"] = info.Name, ["contentBase64"] = Convert.ToBase64String(File.ReadAllBytes(path)) };
        }

        // ---------- programs ----------
        private async Task<JsonObject> RunProgramAsync(JsonObject args, bool script, CancellationToken cancellationToken)
        {
            var requested = Text(args, "path");
            var arguments = Text(args, "arguments");
            ProcessStartInfo info;
            if (script)
            {
                var path = _scriptFolders.Resolve(requested);
                if (!File.Exists(path)) { throw new AgentException("The script was not found."); }
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (!ScriptExtensions.Contains(extension)) { throw new AgentException("Only .ps1, .bat and .cmd scripts can run."); }
                info = extension == ".ps1"
                    ? new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\" {arguments}")
                    : new ProcessStartInfo("cmd.exe", $"/c \"\"{path}\" {arguments}\"");
                info.WorkingDirectory = Path.GetDirectoryName(path)!;
            }
            else
            {
                if (!IsAllowedProgram(requested)) { throw new AgentException("This program is not in the allowed list (allowedPrograms) of Partnerly Agent."); }
                info = new ProcessStartInfo(requested, arguments);
            }

            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var wait = IsTrue(args, "wait");
            info.RedirectStandardOutput = wait;
            info.RedirectStandardError = wait;

            using var process = Process.Start(info) ?? throw new AgentException("The program did not start.");
            if (!wait)
            {
                return new JsonObject { ["started"] = true, ["processId"] = process.Id };
            }

            var seconds = int.TryParse(Text(args, "timeoutSeconds"), out var parsed) && parsed > 0 ? Math.Min(parsed, 600) : 60;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(seconds));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* already finished */ }
                throw new AgentException($"The program did not finish in {seconds} seconds and was stopped.");
            }

            return new JsonObject
            {
                ["exitCode"] = process.ExitCode,
                ["output"] = Cut(await output),
                ["errors"] = Cut(await errors)
            };
        }

        private bool IsAllowedProgram(string requested) =>
            requested.Length > 0 && _config.AllowedPrograms.Any(item =>
                string.Equals(Environment.ExpandEnvironmentVariables(item.Trim()), requested, StringComparison.OrdinalIgnoreCase));

        // Closes a window politely. Nothing is ever killed.
        private JsonObject CloseProgram(JsonObject args)
        {
            var name = StripExe(Text(args, "name"));
            var allowed = _config.AllowedPrograms.Any(item => string.Equals(StripExe(Path.GetFileName(item.Trim())), name, StringComparison.OrdinalIgnoreCase));
            if (name.Length == 0 || !allowed) { throw new AgentException("This program is not in the allowed list (allowedPrograms) of Partnerly Agent."); }
            var closed = 0;
            foreach (var process in Process.GetProcessesByName(name))
            {
                try { if (process.CloseMainWindow()) { closed++; } }
                finally { process.Dispose(); }
            }

            return new JsonObject { ["closed"] = closed };
        }

        // ---------- computer ----------
        private static JsonObject Lock()
        {
            if (!NativeMethods.LockWorkStation()) { throw new AgentException("The PC could not be locked."); }
            return new JsonObject { ["locked"] = true };
        }

        private static JsonObject Sleep()
        {
            // The answer is sent first; the PC falls asleep a moment later.
            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                Application.SetSuspendState(PowerState.Suspend, false, false);
            });
            return new JsonObject { ["sleeping"] = true };
        }

        // ---------- more files ----------
        private JsonObject CreateFolder(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            var existed = Directory.Exists(path);
            Directory.CreateDirectory(path);
            return new JsonObject { ["path"] = path, ["existed"] = existed };
        }

        // Only files, and only into the Recycle Bin: the person can always put them back.
        private JsonObject DeleteFile(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            if (!File.Exists(path)) { throw new AgentException("The file was not found."); }
            if (!NativeMethods.MoveToRecycleBin(path))
            {
                throw new AgentException("Windows could not move the file to the Recycle Bin (is it open in a program?).");
            }

            return new JsonObject { ["deleted"] = true, ["path"] = path };
        }

        private JsonObject FileInformation(JsonObject args)
        {
            var path = _folders.Resolve(Text(args, "path"));
            if (Directory.Exists(path))
            {
                return new JsonObject { ["exists"] = true, ["isFolder"] = true, ["modified"] = Directory.GetLastWriteTimeUtc(path).ToString("O") };
            }

            if (!File.Exists(path))
            {
                return new JsonObject { ["exists"] = false, ["isFolder"] = false };
            }

            var info = new FileInfo(path);
            return new JsonObject
            {
                ["exists"] = true,
                ["isFolder"] = false,
                ["size"] = info.Length,
                ["modified"] = info.LastWriteTimeUtc.ToString("O"),
                ["created"] = info.CreationTimeUtc.ToString("O"),
                ["extension"] = info.Extension.TrimStart('.').ToLowerInvariant()
            };
        }

        private JsonObject Unzip(JsonObject args)
        {
            var source = _folders.Resolve(Text(args, "source"));
            var destination = _folders.Resolve(Text(args, "destination"));
            if (!File.Exists(source)) { throw new AgentException("The archive was not found."); }
            long total = 0;
            var count = 0;
            using (var archive = ZipFile.OpenRead(source))
            {
                foreach (var entry in archive.Entries)
                {
                    total += entry.Length;
                    count++;
                }
            }

            if (total > 500L * 1024 * 1024) { throw new AgentException("The archive is bigger than 500 MB when unpacked."); }
            Directory.CreateDirectory(destination);
            ZipFile.ExtractToDirectory(source, destination, IsTrue(args, "overwrite"));
            return new JsonObject { ["path"] = destination, ["files"] = count };
        }

        // ---------- more information ----------
        private static JsonObject ListPrograms()
        {
            var items = new JsonArray();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero && process.MainWindowTitle.Length > 0 && items.Count < 100)
                    {
                        items.Add(new JsonObject
                        {
                            ["name"] = process.ProcessName,
                            ["title"] = process.MainWindowTitle,
                            ["memoryMb"] = process.WorkingSet64 / 1048576
                        });
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { /* closed while reading */ }
                finally { process.Dispose(); }
            }

            return new JsonObject { ["count"] = items.Count, ["items"] = items };
        }

        private static JsonObject ActiveWindow()
        {
            var (title, processId) = NativeMethods.ActiveWindow();
            var name = string.Empty;
            try { name = Process.GetProcessById(processId).ProcessName; }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { /* window of a closed program */ }
            return new JsonObject { ["title"] = title, ["program"] = name };
        }

        private static JsonObject NetworkInformationResult()
        {
            var addresses = new JsonArray();
            foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                         .Where(a => a.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                                     && a.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback))
            {
                foreach (var ip in adapter.GetIPProperties().UnicastAddresses
                             .Where(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                {
                    addresses.Add(new JsonObject { ["adapter"] = adapter.Name, ["ip"] = ip.Address.ToString() });
                }
            }

            return new JsonObject
            {
                ["hostname"] = Environment.MachineName,
                ["online"] = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable(),
                ["addresses"] = addresses
            };
        }

        private static async Task<JsonObject> PingAsync(JsonObject args)
        {
            var host = Text(args, "host");
            if (host.Length == 0 || host.Length > 253 || host.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '-' or ':')))
            {
                throw new AgentException("Enter a site name or an IP address, for example example.com or 192.168.1.1.");
            }

            using var ping = new System.Net.NetworkInformation.Ping();
            try
            {
                var reply = await ping.SendPingAsync(host, 3000);
                var ok = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                return new JsonObject { ["reachable"] = ok, ["roundTripMs"] = ok ? reply.RoundtripTime : null, ["status"] = reply.Status.ToString() };
            }
            catch (System.Net.NetworkInformation.PingException ex)
            {
                return new JsonObject { ["reachable"] = false, ["roundTripMs"] = null, ["status"] = ex.GetBaseException().Message };
            }
        }

        private async Task<JsonObject> DownloadUrlAsync(JsonObject args, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(Text(args, "url"), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new AgentException("Enter a web address that starts with http:// or https://.");
            }

            var folder = _folders.Resolve(Text(args, "folder"));
            var fileName = Path.GetFileName(Text(args, "fileName"));
            if (fileName.Length == 0) { fileName = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath)); }
            if (fileName.Length == 0) { fileName = "download"; }
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { throw new AgentException("The file name is not valid."); }
            if (ProgramExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            {
                throw new AgentException("Programs and scripts cannot be downloaded this way.");
            }

            var path = _folders.Resolve(Path.Combine(folder, fileName));
            if (File.Exists(path) && !IsTrue(args, "overwrite")) { throw new AgentException("The file already exists. Turn on \"Replace if it exists\" to overwrite."); }
            Directory.CreateDirectory(folder);

            const long limit = 100L * 1024 * 1024;
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode) { throw new AgentException($"The site answered {(int)response.StatusCode}."); }
            if (response.Content.Headers.ContentLength > limit) { throw new AgentException("The file is bigger than 100 MB."); }

            var temp = path + ".part";
            try
            {
                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var target = File.Create(temp))
                {
                    var buffer = new byte[81920];
                    long written = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        written += read;
                        if (written > limit) { throw new AgentException("The file is bigger than 100 MB."); }
                        await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }

                File.Move(temp, path, true);
            }
            finally
            {
                if (File.Exists(temp)) { File.Delete(temp); }
            }

            return new JsonObject { ["path"] = path, ["size"] = new FileInfo(path).Length };
        }

        // ---------- sound, screen, power ----------
        private static JsonObject Volume(JsonObject args)
        {
            // The card shows "Louder" as the first choice, so an untouched card means that.
            var action = Text(args, "action").ToLowerInvariant();
            if (action.Length == 0) { action = "up"; }
            var steps = int.TryParse(Text(args, "steps"), out var parsed) ? Math.Clamp(parsed, 1, 25) : 5;
            switch (action)
            {
                case "mute": NativeMethods.PressMediaKey(0xAD); break;
                case "down": for (var i = 0; i < steps; i++) { NativeMethods.PressMediaKey(0xAE); } break;
                case "up": for (var i = 0; i < steps; i++) { NativeMethods.PressMediaKey(0xAF); } break;
                default: throw new AgentException("Choose: louder, quieter or mute.");
            }

            return new JsonObject { ["done"] = true };
        }

        private static JsonObject Media(JsonObject args)
        {
            var command = Text(args, "action").ToLowerInvariant() switch
            {
                "" or "playpause" => (byte)0xB3,
                "next" => (byte)0xB0,
                "previous" => (byte)0xB1,
                "stop" => (byte)0xB2,
                _ => throw new AgentException("Choose: play/pause, next, previous or stop.")
            };
            NativeMethods.PressMediaKey(command);
            return new JsonObject { ["done"] = true };
        }

        // Only numbers and fixed words reach the command line.
        private static JsonObject Shutdown(JsonObject args)
        {
            var mode = Text(args, "mode").ToLowerInvariant();
            var delay = int.TryParse(Text(args, "delaySeconds"), out var parsed) ? Math.Clamp(parsed, 15, 600) : 60;
            string arguments = mode switch
            {
                "restart" => $"/r /t {delay} /c \"Partnerly: restart in {delay} seconds\"",
                "logoff" => "/l",
                _ => $"/s /t {delay} /c \"Partnerly: shutdown in {delay} seconds\""
            };
            Process.Start(new ProcessStartInfo("shutdown.exe", arguments) { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
            return new JsonObject { ["scheduled"] = true, ["mode"] = mode.Length == 0 ? "shutdown" : mode, ["delaySeconds"] = mode == "logoff" ? 0 : delay };
        }

        private static JsonObject CancelShutdown()
        {
            using var process = Process.Start(new ProcessStartInfo("shutdown.exe", "/a") { UseShellExecute = false, CreateNoWindow = true });
            process?.WaitForExit(5000);
            return new JsonObject { ["cancelled"] = process?.ExitCode == 0 };
        }

        // ---------- helpers ----------
        private static string Text(JsonObject args, string key) => args[key]?.ToString()?.Trim() ?? string.Empty;

        private static bool IsTrue(JsonObject args, string key) =>
            string.Equals(Text(args, key), "true", StringComparison.OrdinalIgnoreCase);

        private static string StripExe(string name) =>
            name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;

        private static string Cut(string text) => text.Length > MaxOutputChars ? text[..MaxOutputChars] : text;

        private static string Describe(JsonObject args)
        {
            var parts = args
                .Where(item => item.Key != "contentBase64" && item.Value != null)
                .Select(item => item.Key + ": " + (item.Value!.ToString().Length > 80 ? item.Value.ToString()[..80] + "…" : item.Value.ToString()));
            return string.Join("\n", parts);
        }
    }
}
