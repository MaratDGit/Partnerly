namespace Partnerly.Agent.UI
{
    // Shown once after the first successful connection: confirms it worked and tells where the program lives.
    public sealed class WelcomeForm : Form
    {
        public bool OpenSettings { get; private set; }

        public WelcomeForm(string machine, string server, string firstFolder)
        {
            Text = "Partnerly Agent";
            Icon = TrayIcons.App();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
            ClientSize = new Size(500, 330);
            Padding = new Padding(16);

            var title = new Label
            {
                Text = "Done - this computer is connected",
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Dock = DockStyle.Top,
                Height = 36
            };
            var body = new Label
            {
                Dock = DockStyle.Top,
                Height = 190,
                Text = $"\"{machine}\" is now linked to {server}.\n\n" +
                       "Where is the program?\n" +
                       "   It lives near the clock (the tray). If you do not see the icon, click the small arrow ^ next to the clock.\n" +
                       "   A green dot on the icon means \"connected\". Right-click the icon for the menu: turn on / off, settings, remove.\n\n" +
                       "What may Partnerly do now?\n" +
                       $"   Work with files only in:  {firstFolder}\n" +
                       "   Add more folders and programs in  Settings  (tray menu)."
            };
            var settings = new Button { Text = "Open settings", Width = 130, Height = 34 };
            settings.Click += (_, _) => { OpenSettings = true; Close(); };
            var ok = new Button { Text = "OK", Width = 100, Height = 34, DialogResult = DialogResult.OK };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(settings);
            AcceptButton = ok;

            Controls.Add(buttons);
            Controls.Add(body);
            Controls.Add(title);
        }
    }
}
