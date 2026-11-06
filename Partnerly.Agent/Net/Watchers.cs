using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using Partnerly.Agent.Commands;
using Partnerly.Agent.Config;
using Partnerly.Agent.UI;

namespace Partnerly.Agent.Net
{
    // Watches what happens on this PC and tells the site. Only things the settings allow are
    // watched; a folder or drive that appears later (a USB stick, a network drive) is picked up
    // by the periodic check.
    public sealed class Watchers : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly AgentConnection _connection;
        private readonly UiBridge _ui;
        private readonly Dictionary<string, FileSystemWatcher> _folders = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DateTime> _recent = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _changing = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _stop;
        private HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _drives = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _lowDisks = new(StringComparer.OrdinalIgnoreCase);
        private bool? _onBattery;
        private int _batteryLevelSent = 101;
        private bool _idle;
        private string _clipboard = string.Empty;
        private bool _clipboardKnown;
        private bool _subscribed;

        public Watchers(AgentConfig config, AgentConnection connection, UiBridge ui)
        {
            _config = config;
            _connection = connection;
            _ui = ui;
        }

        public void Start()
        {
            Stop();
            _stop = new CancellationTokenSource();
            var token = _stop.Token;
            RefreshFolders();
            _seen = CurrentProgramNames();
            _drives = CurrentDrives();
            if (!_subscribed)
            {
                SystemEvents.SessionSwitch += OnSession;
                SystemEvents.PowerModeChanged += OnPower;
                _subscribed = true;
            }

            _ = Task.Run(() => LoopAsync(token));
        }

        public void Stop()
        {
            _stop?.Cancel();
            _stop?.Dispose();
            _stop = null;
            if (_subscribed)
            {
                SystemEvents.SessionSwitch -= OnSession;
                SystemEvents.PowerModeChanged -= OnPower;
                _subscribed = false;
            }

            foreach (var watcher in _folders.Values)
            {
                watcher.Dispose();
            }

            _folders.Clear();
        }

        // One loop does all the polling; each job has its own rhythm.
        private async Task LoopAsync(CancellationToken token)
        {
            var tick = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                    tick++;
                    if (_config.WatchPrograms.Count > 0) { await CheckProgramsAsync(); }
                    if (_config.WatchClipboard && tick % 1 == 0) { await CheckClipboardAsync(); }
                    if (tick % 2 == 0) { await CheckDrivesAsync(); }
                    if (tick % 5 == 0 && _config.IdleMinutes > 0) { await CheckIdleAsync(); }
                    if (tick % 10 == 0) { RefreshFolders(); await CheckBatteryAsync(); }
                    if (tick % 100 == 0 && _config.DiskLowGb > 0) { await CheckDisksAsync(); }
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { AgentLog.Write("Watching failed: " + ex.Message); }
            }
        }

        // ---------- folders ----------
        private void RefreshFolders()
        {
            var wanted = _config.WatchFolders
                .Select(Environment.ExpandEnvironmentVariables)
                .Where(path => path.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var gone in _folders.Keys.Where(path => !wanted.Contains(path) || !Directory.Exists(path)).ToList())
            {
                _folders[gone].Dispose();
                _folders.Remove(gone);
                AgentLog.Write("Stopped watching " + gone);
            }

            foreach (var folder in wanted.Where(path => !_folders.ContainsKey(path) && Directory.Exists(path)))
            {
                try
                {
                    var watcher = new FileSystemWatcher(folder)
                    {
                        IncludeSubdirectories = _config.WatchSubfolders,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        InternalBufferSize = 64 * 1024,
                        EnableRaisingEvents = true
                    };
                    watcher.Created += (_, e) => _ = ReportFileAsync(e.FullPath, "file_created");
                    watcher.Changed += (_, e) => _ = ReportChangedAsync(e.FullPath);
                    watcher.Renamed += (_, e) => _ = ReportFileAsync(e.FullPath, "file_created");
                    watcher.Deleted += (_, e) => _ = ReportDeletedAsync(e.FullPath);
                    watcher.Error += (_, e) =>
                    {
                        // The buffer overflowed or the folder vanished: drop it, the next check starts it again.
                        AgentLog.Write("Folder watch error in " + folder + ": " + e.GetException().Message);
                        if (_folders.Remove(folder, out var broken)) { broken.Dispose(); }
                    };
                    _folders[folder] = watcher;
                    AgentLog.Write("Watching folder " + folder);
                }
                catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
                {
                    AgentLog.Write("Cannot watch " + folder + ": " + ex.Message);
                }
            }
        }

        // The file may still be written when the event arrives: wait until it can be opened.
        private async Task ReportFileAsync(string path, string type)
        {
            var token = _stop?.Token ?? CancellationToken.None;
            try
            {
                _recent[path] = DateTime.UtcNow;
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(500, token);
                    if (Directory.Exists(path) || !File.Exists(path))
                    {
                        return;
                    }

                    try
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        await _connection.SendEventAsync(type, FilePayload(path));
                        return;
                    }
                    catch (IOException) { /* still being written */ }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AgentLog.Write("File event failed: " + ex.Message);
            }
        }

        // A save fires Changed many times: report once, after things were quiet for a moment.
        private async Task ReportChangedAsync(string path)
        {
            if (_recent.TryGetValue(path, out var created) && DateTime.UtcNow - created < TimeSpan.FromSeconds(6))
            {
                return;
            }

            if (!_changing.TryAdd(path, true))
            {
                return;
            }

            try
            {
                var token = _stop?.Token ?? CancellationToken.None;
                DateTime? last = null;
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(1500, token);
                    if (Directory.Exists(path) || !File.Exists(path)) { return; }
                    var stamp = File.GetLastWriteTimeUtc(path);
                    if (last == stamp)
                    {
                        await _connection.SendEventAsync("file_changed", FilePayload(path));
                        return;
                    }

                    last = stamp;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AgentLog.Write("File change event failed: " + ex.Message);
            }
            finally
            {
                _changing.TryRemove(path, out _);
            }
        }

        private async Task ReportDeletedAsync(string path)
        {
            _recent.TryRemove(path, out _);
            await _connection.SendEventAsync("file_deleted", new JsonObject
            {
                ["path"] = path,
                ["fileName"] = Path.GetFileName(path),
                ["folder"] = Path.GetDirectoryName(path),
                ["extension"] = Path.GetExtension(path).TrimStart('.').ToLowerInvariant()
            });
        }

        private static JsonObject FilePayload(string path)
        {
            var info = new FileInfo(path);
            return new JsonObject
            {
                ["path"] = path,
                ["fileName"] = info.Name,
                ["folder"] = info.DirectoryName,
                ["extension"] = info.Extension.TrimStart('.').ToLowerInvariant(),
                ["size"] = info.Length
            };
        }

        // ---------- programs ----------
        private async Task CheckProgramsAsync()
        {
            var wanted = new HashSet<string>(_config.WatchPrograms.Select(StripExe), StringComparer.OrdinalIgnoreCase);
            var now = CurrentProgramNames();
            foreach (var name in now.Where(name => wanted.Contains(name) && !_seen.Contains(name)))
            {
                await _connection.SendEventAsync("program_started", new JsonObject { ["name"] = name });
            }

            foreach (var name in _seen.Where(name => wanted.Contains(name) && !now.Contains(name)))
            {
                await _connection.SendEventAsync("program_stopped", new JsonObject { ["name"] = name });
            }

            _seen = now;
        }

        private static HashSet<string> CurrentProgramNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                names.Add(process.ProcessName);
                process.Dispose();
            }

            return names;
        }

        // ---------- screen lock, power ----------
        private void OnSession(object? sender, SessionSwitchEventArgs e)
        {
            var state = e.Reason switch
            {
                SessionSwitchReason.SessionLock => "locked",
                SessionSwitchReason.SessionUnlock => "unlocked",
                SessionSwitchReason.SessionLogon => "signed_in",
                SessionSwitchReason.SessionLogoff => "signed_out",
                _ => null
            };
            if (state != null)
            {
                _ = _connection.SendEventAsync("session_changed", new JsonObject { ["state"] = state });
            }
        }

        private void OnPower(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                // After sleep the picture of the world is old: forget it so nothing is reported twice.
                _seen = CurrentProgramNames();
                _drives = CurrentDrives();
                _ = _connection.SendEventAsync("session_changed", new JsonObject { ["state"] = "woke_up" });
            }
            else if (e.Mode == PowerModes.Suspend)
            {
                _ = _connection.SendEventAsync("session_changed", new JsonObject { ["state"] = "going_to_sleep" });
            }
        }

        private async Task CheckBatteryAsync()
        {
            var power = SystemInformation.PowerStatus;
            if (power.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                return;
            }

            var onBattery = power.PowerLineStatus == PowerLineStatus.Offline;
            var percent = power.BatteryLifePercent is >= 0f and <= 1f ? (int)Math.Round(power.BatteryLifePercent * 100) : -1;
            if (_onBattery != null && _onBattery != onBattery)
            {
                await _connection.SendEventAsync("power_changed", new JsonObject
                {
                    ["state"] = onBattery ? "unplugged" : "plugged_in",
                    ["percent"] = percent
                });
            }

            _onBattery = onBattery;
            if (!onBattery)
            {
                _batteryLevelSent = 101;
                return;
            }

            foreach (var level in new[] { 20, 10, 5 })
            {
                if (percent >= 0 && percent <= level && _batteryLevelSent > level)
                {
                    _batteryLevelSent = level;
                    await _connection.SendEventAsync("power_changed", new JsonObject { ["state"] = "battery_low", ["percent"] = percent });
                    break;
                }
            }
        }

        // ---------- clipboard, idle, drives, disks ----------
        private async Task CheckClipboardAsync()
        {
            string text;
            try { text = _ui.GetClipboard(); }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException) { return; }

            if (!_clipboardKnown)
            {
                _clipboard = text;
                _clipboardKnown = true;
                return;
            }

            if (text.Length > 0 && text != _clipboard)
            {
                _clipboard = text;
                await _connection.SendEventAsync("clipboard_changed", new JsonObject
                {
                    ["text"] = text.Length > 2000 ? text[..2000] : text,
                    ["length"] = text.Length
                });
            }
        }

        private async Task CheckIdleAsync()
        {
            var idleMinutes = NativeMethods.IdleMinutes();
            if (!_idle && idleMinutes >= _config.IdleMinutes)
            {
                _idle = true;
                await _connection.SendEventAsync("idle_changed", new JsonObject { ["state"] = "idle", ["idleMinutes"] = (int)idleMinutes });
            }
            else if (_idle && idleMinutes < 1)
            {
                _idle = false;
                await _connection.SendEventAsync("idle_changed", new JsonObject { ["state"] = "active", ["idleMinutes"] = 0 });
            }
        }

        private static HashSet<string> CurrentDrives()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType is DriveType.Removable or DriveType.Network or DriveType.CDRom))
                {
                    names.Add(drive.Name);
                }
            }
            catch (IOException) { /* a drive vanished while listing */ }

            return names;
        }

        private async Task CheckDrivesAsync()
        {
            var now = CurrentDrives();
            foreach (var name in now.Where(name => !_drives.Contains(name)))
            {
                var label = string.Empty;
                var kind = "removable";
                try
                {
                    var drive = new DriveInfo(name);
                    label = drive.VolumeLabel;
                    kind = drive.DriveType.ToString().ToLowerInvariant();
                }
                catch (IOException) { /* gone again */ }

                await _connection.SendEventAsync("drive_changed", new JsonObject { ["state"] = "connected", ["drive"] = name, ["label"] = label, ["kind"] = kind });
            }

            foreach (var name in _drives.Where(name => !now.Contains(name)))
            {
                await _connection.SendEventAsync("drive_changed", new JsonObject { ["state"] = "removed", ["drive"] = name, ["label"] = string.Empty, ["kind"] = "removable" });
            }

            _drives = now;
        }

        private async Task CheckDisksAsync()
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                var freeGb = drive.AvailableFreeSpace / 1073741824.0;
                if (freeGb < _config.DiskLowGb)
                {
                    if (_lowDisks.Add(drive.Name))
                    {
                        await _connection.SendEventAsync("disk_low", new JsonObject
                        {
                            ["drive"] = drive.Name,
                            ["freeGb"] = Math.Round(freeGb, 1),
                            ["freePercent"] = Math.Round(100.0 * drive.AvailableFreeSpace / drive.TotalSize, 1)
                        });
                    }
                }
                else
                {
                    _lowDisks.Remove(drive.Name);
                }
            }
        }

        private static string StripExe(string name) =>
            name.Trim().EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Trim()[..^4] : name.Trim();

        public void Dispose() => Stop();
    }
}
