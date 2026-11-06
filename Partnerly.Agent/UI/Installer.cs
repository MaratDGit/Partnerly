using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using Partnerly.Agent.Config;
using Partnerly.Agent.Net;

namespace Partnerly.Agent.UI
{
    // Start-up steps of a freshly downloaded program: refuse old Windows, notice a program that
    // was started inside the zip, take over the settings that came in the zip, and offer to
    // install for the current user. Also the way back: uninstall.
    public static class Installer
    {
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\PartnerlyAgent";

        public static string InstallFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PartnerlyAgent");

        private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

        private static string InstalledExe => Path.Combine(InstallFolder, "Partnerly.Agent.exe");

        public static bool IsInstalled =>
            string.Equals(Path.GetDirectoryName(ExePath), InstallFolder, StringComparison.OrdinalIgnoreCase);

        // Windows 10 or newer is needed (the program is built for it).
        public static bool WindowsIsSupported() => Environment.OSVersion.Version.Major >= 10;

        // Explorer shows a zip like a folder and starts programs from a temporary copy of ONLY that
        // file: config.json is not there, the program asks for a token, the person is lost.
        // Returns true (after explaining) when this is the case and the program should close.
        public static bool StartedInsideArchive()
        {
            if (IsInstalled || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME")))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(ExePath) ?? string.Empty;
            var temp = Path.GetTempPath();
            var inTemp = folder.StartsWith(temp.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
            if (!inTemp || File.Exists(Path.Combine(folder, "config.json")) || AgentConfig.Load() != null)
            {
                return false;
            }

            MessageBox.Show(
                "The program was started from inside the zip file, so your settings are not available.\n\n" +
                "1. Close this window.\n" +
                "2. Right-click the downloaded Partnerly-Agent.zip and choose \"Extract All\".\n" +
                "3. Open the new folder and start Partnerly.Agent.exe from there.",
                "Partnerly Agent - unpack the zip first",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return true;
        }

        // config.json next to the program (made by the site with the address and token of this
        // computer) becomes the real settings, and the loose copy is removed because it holds the token.
        public static void ImportSettingsFromZip()
        {
            var loose = Path.Combine(Path.GetDirectoryName(ExePath) ?? string.Empty, "config.json");
            if (!File.Exists(loose) || string.Equals(loose, AgentConfig.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                if (JsonNode.Parse(File.ReadAllText(loose)) is JsonObject node
                    && node["server"]?.ToString() is { Length: > 0 } server
                    && node["token"]?.ToString() is { Length: > 0 } token)
                {
                    var config = AgentConfig.Load() ?? AgentConfig.CreateDefault(server, token);
                    config.Server = server.Trim().TrimEnd('/');
                    config.Token = token.Trim();
                    config.Enabled = true;
                    if (config.AllowedFolders.Count == 0)
                    {
                        config.AllowedFolders = AgentConfig.CreateDefault(server, token).AllowedFolders;
                    }

                    config.Save();
                    File.Delete(loose);
                    AgentLog.Write("Settings were imported from config.json next to the program.");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                AgentLog.Write("config.json next to the program could not be used: " + ex.Message);
            }
        }

        // An older installed copy that is still running would block the update: close it first.
        private static void StopInstalledCopy(string target)
        {
            foreach (var process in Process.GetProcessesByName("Partnerly.Agent"))
            {
                try
                {
                    if (process.Id != Environment.ProcessId
                        && string.Equals(process.MainModule?.FileName, target, StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // not ours to stop, or already gone
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        // True when an installed copy was started and this one must close.
        public static bool OfferInstall()
        {
            // A portable copy (settings folder chosen by PARTNERLY_AGENT_HOME) is never installed.
            if (IsInstalled || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME")))
            {
                return false;
            }

            var update = File.Exists(InstalledExe);
            var answer = MessageBox.Show(
                (update
                    ? "Partnerly Agent is already installed on this computer.\n\nReplace it with this version?"
                    : "Install Partnerly Agent for your Windows account?")
                + "\n\nIt is copied to your user folder (no administrator rights needed) and starts with Windows. "
                + "You can turn that off in the tray menu, and remove the program in Windows Settings > Apps.\n\n"
                + "If you press No, the program runs from the folder it is in now - do not delete that folder.",
                "Partnerly Agent",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(InstallFolder);
                StopInstalledCopy(InstalledExe);
                CopyWithRetry(ExePath, InstalledExe);
                TrayApp.SetStartup(true, InstalledExe);
                RegisterUninstall();
                Process.Start(new ProcessStartInfo(InstalledExe) { UseShellExecute = true });
                AgentLog.Write("Installed to " + InstallFolder);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("The program could not be installed: " + ex.Message + "\n\nIt will run from where it is now.", "Partnerly Agent");
                return false;
            }
        }

        // The old copy may need a moment to let go of the file.
        private static void CopyWithRetry(string from, string to)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    File.Copy(from, to, overwrite: true);
                    return;
                }
                catch (IOException) when (attempt < 6)
                {
                    Thread.Sleep(500);
                }
            }
        }

        // Makes the program appear in Windows Settings > Apps, so it can be removed the usual way.
        public static void RegisterUninstall()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(UninstallKey);
                key.SetValue("DisplayName", "Partnerly Agent");
                key.SetValue("DisplayVersion", typeof(Installer).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
                key.SetValue("Publisher", "Partnerly");
                key.SetValue("DisplayIcon", InstalledExe);
                key.SetValue("InstallLocation", InstallFolder);
                key.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                AgentLog.Write("The program could not be added to the Windows app list: " + ex.Message);
            }
        }

        // When an installed copy starts, repair what a clean-up tool may have removed
        // (the "Apps" entry, the start-with-Windows entry that points to the wrong place).
        public static void SelfHeal(AgentConfig config)
        {
            if (!IsInstalled)
            {
                return;
            }

            RegisterUninstall();
            if (TrayApp.IsStartup() && !string.Equals(TrayApp.StartupTarget(), InstalledExe, StringComparison.OrdinalIgnoreCase))
            {
                TrayApp.SetStartup(true, InstalledExe);
            }
        }

        // Removes the program from this PC. Returns true when the program should close now.
        public static bool Uninstall(bool askFirst, Action? beforeRemoving = null)
        {
            if (askFirst
                && MessageBox.Show("Remove Partnerly Agent from this computer?\n\nCards of your agents that use this computer will stop working until you install it again.",
                    "Partnerly Agent - remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return false;
            }

            var deleteSettings = MessageBox.Show("Also delete the settings and the log of this computer?\n\n" +
                "Choose No to keep them (handy if you install the program again).",
                "Partnerly Agent - remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

            beforeRemoving?.Invoke();
            TrayApp.SetStartup(false);
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { /* nothing to remove */ }

            StopInstalledCopy(InstalledExe);
            if (deleteSettings)
            {
                try { Directory.Delete(AgentConfig.Folder, recursive: true); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* the log is still open */ }
            }

            // A program cannot delete its own file: a hidden helper waits a few seconds, then removes the folder.
            if (Directory.Exists(InstallFolder) && InstallFolder.EndsWith("PartnerlyAgent", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 4 >nul & rmdir /s /q \"" + InstallFolder + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.Dispose();
            }

            MessageBox.Show("Partnerly Agent was removed. You can delete the computer on the site's Integrations page too.", "Partnerly Agent");
            return true;
        }
    }
}
