using System.Diagnostics;
using Microsoft.Win32;
using Partnerly.Agent.Commands;
using Partnerly.Agent.Config;
using Partnerly.Agent.Net;

namespace Partnerly.Agent.UI
{
    // The program lives in the tray: status icon, on/off switch, settings, removal.
    public sealed class TrayApp : ApplicationContext
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunName = "PartnerlyAgent";

        private readonly AgentConfig _config;
        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _status;
        private readonly ToolStripMenuItem _enabled;
        private readonly AgentConnection _connection;
        private readonly Watchers _watchers;
        private readonly System.Windows.Forms.Timer _help = new() { Interval = 30000 };
        private bool _everConnected;

        public TrayApp(AgentConfig config)
        {
            _config = config;
            _menu = new ContextMenuStrip();
            _status = new ToolStripMenuItem("Connecting...") { Enabled = false };
            _enabled = new ToolStripMenuItem("Let Partnerly use this computer") { Checked = config.Enabled, CheckOnClick = true };
            _enabled.Click += async (_, _) => await ToggleAsync(_enabled.Checked);
            var startup = new ToolStripMenuItem("Start with Windows") { Checked = IsStartup(), CheckOnClick = true };
            startup.CheckedChanged += (_, _) => SetStartup(startup.Checked);

            _menu.Items.Add(_status);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_enabled);
            _menu.Items.Add("Settings - folders, programs, watching...", null, (_, _) => ShowSettings());
            _menu.Items.Add("Connection - site address and token...", null, (_, _) => ShowConnection());
            _menu.Items.Add("Check connection", null, async (_, _) => await CheckAsync());
            _menu.Items.Add("Open log", null, (_, _) => Open(AgentConfig.LogPath));
            _menu.Items.Add(startup);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("About", null, (_, _) => About());
            _menu.Items.Add("Remove program from this computer...", null, (_, _) => Remove());
            _menu.Items.Add("Exit", null, (_, _) => ExitThread());

            _tray = new NotifyIcon
            {
                Icon = TrayIcons.For(AgentState.Connecting),
                Text = "Partnerly Agent",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _tray.DoubleClick += (_, _) => ShowSettings();
            _menu.CreateControl();
            _ = _menu.Handle; // a handle on the UI thread makes BeginInvoke from other threads work

            var ui = new UiBridge(_tray);
            _connection = new AgentConnection(config, new CommandRunner(config, ui));
            _connection.StatusChanged += (text, state) => _menu.BeginInvoke(() => ShowStatus(text, state));
            _connection.FirstConnected += () => _menu.BeginInvoke(OnConnected);
            _watchers = new Watchers(config, _connection, ui);

            // A person who sees "Offline" for half a minute gets a hint instead of silence.
            _help.Tick += (_, _) =>
            {
                _help.Stop();
                if (!_everConnected && _config.Enabled && _connection.State != AgentState.Paused)
                {
                    var reason = _connection.LastProblem.Length > 0 ? _connection.LastProblem : "it is still trying";
                    _tray.ShowBalloonTip(15000, "Partnerly Agent is not connected yet", "Reason: " + reason + ".\nRight-click the icon > Check connection.", ToolTipIcon.Warning);
                }
            };
            _help.Start();

            _ = Task.Run(() => _connection.RunAsync());
        }

        private void ShowStatus(string text, AgentState state)
        {
            _status.Text = text;
            _tray.Icon = TrayIcons.For(state);
            var tip = "Partnerly Agent - " + text;
            _tray.Text = tip.Length > 63 ? tip[..63] : tip;
        }

        private void OnConnected()
        {
            _everConnected = true;
            _watchers.Start();
            if (!_config.Welcomed)
            {
                _config.Welcomed = true;
                try { _config.Save(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* shown again next time */ }
                using var welcome = new WelcomeForm(Environment.MachineName, _config.Server, _config.AllowedFolders.FirstOrDefault() ?? "(none yet)");
                welcome.ShowDialog();
                if (welcome.OpenSettings) { ShowSettings(); }
            }
        }

        // The master switch: off means disconnected and silent, not just "refusing".
        private async Task ToggleAsync(bool on)
        {
            _config.Enabled = on;
            try { _config.Save(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* the switch still works for this run */ }
            if (on)
            {
                await _connection.ResumeAsync();
            }
            else
            {
                _watchers.Stop();
                await _connection.PauseAsync();
            }
        }

        private void ShowSettings()
        {
            using var form = new SettingsForm(_config);
            if (form.ShowDialog() == DialogResult.OK)
            {
                Program.Restart();
            }
        }

        private void ShowConnection()
        {
            using var form = new SetupForm(_config);
            if (form.ShowDialog() == DialogResult.OK)
            {
                Program.Restart();
            }
        }

        private async Task CheckAsync()
        {
            _status.Text = "Checking...";
            var report = await Diagnostics.RunAsync(_config, _connection);
            MessageBox.Show(report, "Partnerly Agent - check connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void About()
        {
            var version = typeof(TrayApp).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
            MessageBox.Show(
                $"Partnerly Agent {version}\n\nSite: {_config.Server}\nComputer: {Environment.MachineName}\nSettings and log: {AgentConfig.Folder}\nProgram: {Environment.ProcessPath}",
                "Partnerly Agent");
        }

        private void Remove()
        {
            if (Installer.Uninstall(askFirst: true, beforeRemoving: () => _connection.PauseAsync("uninstalled").Wait(4000)))
            {
                ExitThread();
            }
        }

        private static void Open(string path)
        {
            try
            {
                if (!File.Exists(path)) { File.WriteAllText(path, string.Empty); }
                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + path + "\"") { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(ex.Message, "Partnerly Agent");
            }
        }

        public static bool IsStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunName) != null;
        }

        public static string? StartupTarget()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return (key?.GetValue(RunName) as string)?.Trim('"');
        }

        internal static void SetStartup(bool on, string? exePath = null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) { return; }
            if (on) { key.SetValue(RunName, "\"" + (exePath ?? Environment.ProcessPath ?? Application.ExecutablePath) + "\""); }
            else { key.DeleteValue(RunName, throwOnMissingValue: false); }
        }

        protected override void ExitThreadCore()
        {
            _help.Stop();
            _tray.Visible = false;
            _watchers.Dispose();
            _connection.DisposeAsync().AsTask().Wait(2000);
            _tray.Dispose();
            base.ExitThreadCore();
        }
    }
}
