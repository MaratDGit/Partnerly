using Partnerly.Agent.Config;

namespace Partnerly.Agent.UI
{
    // Asks for the site address and the token. Used on the first start without settings,
    // and from the tray menu ("Connection...") after the token was renewed on the site.
    public sealed class SetupForm : Form
    {
        private readonly TextBox _server = new() { Dock = DockStyle.Top, PlaceholderText = "https://your-site.example" };
        private readonly TextBox _token = new() { Dock = DockStyle.Top, PlaceholderText = "pc...." };
        private readonly AgentConfig? _existing;

        public AgentConfig? Result { get; private set; }

        public SetupForm(AgentConfig? existing = null)
        {
            _existing = existing;
            Text = "Partnerly Agent - connect this computer";
            Icon = TrayIcons.App();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(480, 280);
            Padding = new Padding(12);

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 82,
                Text = "On the site open  Integrations > Computer.\n" +
                       "Press \"New token\" (key button) on your computer, then copy the address and the token here.\n" +
                       "Or download the program with these settings again from that window - then nothing has to be typed."
            };
            var save = new Button { Text = "Connect", DialogResult = DialogResult.None, Dock = DockStyle.Bottom, Height = 38 };
            save.Click += (_, _) => Accept();
            AcceptButton = save;

            if (existing != null)
            {
                _server.Text = existing.Server;
            }

            Controls.Add(save);
            Controls.Add(Wrap(_token, "Token (starts with pc.)"));
            Controls.Add(Wrap(_server, "Site address"));
            Controls.Add(info);
        }

        private static Control Wrap(TextBox box, string caption)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 58 };
            panel.Controls.Add(box);
            panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = 20 });
            return panel;
        }

        private void Accept()
        {
            var server = _server.Text.Trim();
            if (!Uri.TryCreate(server, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                MessageBox.Show("Enter the address of the site, for example https://example.com\n\nIt must start with https:// (or http://).", "Partnerly Agent");
                return;
            }

            var token = _token.Text.Trim();
            if (!token.StartsWith("pc.", StringComparison.Ordinal) || token.Contains(' '))
            {
                MessageBox.Show("The token starts with \"pc.\" and has no spaces - copy it again from the site (the whole line).", "Partnerly Agent");
                return;
            }

            // Everything else this PC allowed (folders, programs) stays as it was.
            Result = _existing ?? AgentConfig.CreateDefault(server, token);
            Result.Server = server.TrimEnd('/');
            Result.Token = token;
            Result.Enabled = true;
            Result.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
