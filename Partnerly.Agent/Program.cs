using System.Diagnostics;
using Partnerly.Agent.Config;
using Partnerly.Agent.Net;
using Partnerly.Agent.UI;

namespace Partnerly.Agent
{
    internal static class Program
    {
        // A portable copy (own settings folder) may run next to the installed one.
        private static string HomeSuffix() =>
            Environment.GetEnvironmentVariable("PARTNERLY_AGENT_HOME") is { Length: > 0 } home
                ? "." + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(home.ToLowerInvariant())))[..12]
                : string.Empty;

        // Starts a new copy that waits for this one to finish, then closes this one.
        // Used after the settings or the token changed, so everything is read again.
        public static void Restart()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? Application.ExecutablePath, "--restart") { UseShellExecute = false })?.Dispose();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                AgentLog.Write("Restart failed: " + ex.Message);
                return;
            }

            Application.Exit();
        }

        [STAThread]
        private static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Whatever goes wrong inside the program is written to the log; it must not just vanish.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => AgentLog.Write("Unexpected error: " + e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => AgentLog.Write("Fatal error: " + e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (_, e) => { AgentLog.Write("Background error: " + e.Exception.GetBaseException().Message); e.SetObserved(); };

            if (!Installer.WindowsIsSupported())
            {
                MessageBox.Show("Partnerly Agent needs Windows 10 or newer. Please update Windows and start the program again.", "Partnerly Agent");
                return;
            }

            // "Remove" in Windows Settings > Apps starts the installed copy with this switch.
            if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            {
                Installer.Uninstall(askFirst: true);
                return;
            }

            if (Installer.StartedInsideArchive())
            {
                return;
            }

            var restarting = args.Contains("--restart", StringComparer.OrdinalIgnoreCase);
            if (!restarting)
            {
                Installer.ImportSettingsFromZip();
                if (Installer.OfferInstall())
                {
                    return;
                }
            }

            using var single = new Mutex(false, "Partnerly.Agent.SingleInstance" + HomeSuffix());
            var owned = false;
            try
            {
                // After a restart the old copy needs a moment to close.
                try { owned = single.WaitOne(restarting ? TimeSpan.FromSeconds(20) : TimeSpan.Zero); }
                catch (AbandonedMutexException) { owned = true; }

                if (!owned)
                {
                    if (!restarting)
                    {
                        MessageBox.Show("Partnerly Agent is already running.\n\nLook for its icon near the clock (click the small arrow ^ if you do not see it), right-click it for the menu.", "Partnerly Agent");
                    }

                    return;
                }

                RunTray();
            }
            finally
            {
                if (owned) { single.ReleaseMutex(); }
            }
        }

        private static void RunTray()
        {
            var config = AgentConfig.Load();
            if (AgentConfig.LoadProblem == "restored")
            {
                MessageBox.Show("The settings file was damaged. The last good copy was restored.\n\nCheck Settings in the tray menu.", "Partnerly Agent");
            }
            else if (config == null && AgentConfig.LoadProblem != null)
            {
                MessageBox.Show("The settings file is damaged and cannot be read.\n\nEnter the site address and the token again. Folders and programs have to be chosen again in Settings.", "Partnerly Agent");
            }

            if (config == null || string.IsNullOrWhiteSpace(config.Server) || string.IsNullOrWhiteSpace(config.Token))
            {
                using var setup = new SetupForm(config);
                if (setup.ShowDialog() != DialogResult.OK || setup.Result == null)
                {
                    return;
                }

                config = setup.Result;
            }

            Installer.SelfHeal(config);
            AgentLog.Write("Partnerly Agent started.");
            Application.Run(new TrayApp(config));
        }
    }
}
