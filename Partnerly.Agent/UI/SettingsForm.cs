using Partnerly.Agent.Config;

namespace Partnerly.Agent.UI
{
    // A list with "Add..." and "Remove": the settings are chosen with a mouse, not typed into a file.
    internal sealed class ListEditor : UserControl
    {
        private readonly ListBox _list = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, SelectionMode = SelectionMode.MultiExtended };

        public ListEditor(string addCaption, Func<string?> pick, IEnumerable<string> items)
        {
            Dock = DockStyle.Fill;
            foreach (var item in items) { _list.Items.Add(item); }

            var add = new Button { Text = addCaption, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
            add.Click += (_, _) =>
            {
                var value = pick();
                if (!string.IsNullOrWhiteSpace(value) && !_list.Items.Cast<string>().Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    _list.Items.Add(value.Trim());
                }
            };
            var remove = new Button { Text = "Remove selected", AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
            remove.Click += (_, _) =>
            {
                foreach (var selected in _list.SelectedItems.Cast<object>().ToList()) { _list.Items.Remove(selected); }
            };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, FlowDirection = FlowDirection.LeftToRight };
            buttons.Controls.Add(add);
            buttons.Controls.Add(remove);
            Controls.Add(_list);
            Controls.Add(buttons);
        }

        public List<string> Items => _list.Items.Cast<string>().ToList();
    }

    internal sealed class PromptForm : Form
    {
        private readonly TextBox _box = new() { Dock = DockStyle.Top };

        public PromptForm(string title, string question, string example)
        {
            Text = title;
            Icon = TrayIcons.App();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 150);
            Padding = new Padding(12);
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 34 };
            AcceptButton = ok;
            _box.PlaceholderText = example;
            Controls.Add(ok);
            Controls.Add(_box);
            Controls.Add(new Label { Text = question, Dock = DockStyle.Top, Height = 54 });
        }

        public string Value => _box.Text.Trim();

        public static string? Ask(string title, string question, string example)
        {
            using var form = new PromptForm(title, question, example);
            return form.ShowDialog() == DialogResult.OK && form.Value.Length > 0 ? form.Value : null;
        }
    }

    // What this PC lets Partnerly do. Saved to the settings file; the program restarts itself after saving.
    // Three tabs only: the folders, the programs, and what must ask first. Git, scripts and watching need
    // no settings of their own: they work inside the allowed folders.
    public sealed class SettingsForm : Form
    {
        // Special lines of the "ask first" list that are not plain commands.
        private const string GitPushLine = "git_push";
        private const string GitRiskyLine = "git_risky";

        // Commands that can ask for a click first: code -> words a person understands.
        private static readonly (string Op, string Caption)[] Ask =
        {
            ("run_program", "Run a program"),
            ("run_script", "Run a script (.ps1, .bat, .cmd)"),
            ("shutdown", "Shut down or restart the PC"),
            ("delete_file", "Delete a file (to the Recycle Bin)"),
            ("write_file", "Write a text file"),
            ("save_file", "Save a file sent by Partnerly"),
            ("download_url", "Download a file from the internet"),
            ("close_program", "Close a program"),
            ("lock", "Lock the PC"),
            ("sleep", "Put the PC to sleep"),
            ("display_off", "Turn the screen off"),
            ("screenshot", "Take a screenshot"),
            ("get_file", "Send a file to Partnerly"),
            ("clipboard_get", "Read the clipboard"),
            ("open", "Open a link, file or folder"),
            (GitPushLine, "Git: send commits to the internet (push)"),
            (GitRiskyLine, "Git: commands that can lose work (reset --hard, push --force, rebase, clean...)")
        };

        private readonly AgentConfig _config;
        private readonly ListEditor _folders;
        private readonly ListEditor _programs;
        private readonly CheckBox _clipboard = new() { Text = "Tell Partnerly when I copy text (it may include private text)", AutoSize = true };
        private readonly NumericUpDown _idle = new() { Minimum = 0, Maximum = 600, Width = 70 };
        private readonly NumericUpDown _disk = new() { Minimum = 0, Maximum = 5000, Width = 70 };
        private readonly CheckedListBox _confirm = new() { Dock = DockStyle.Fill, CheckOnClick = true };

        public SettingsForm(AgentConfig config)
        {
            _config = config;
            Text = "Partnerly Agent - what Partnerly may do on this PC";
            Icon = TrayIcons.App();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 500);
            MinimumSize = new Size(560, 440);

            _folders = new ListEditor("Add folder...", PickFolder, config.AllowedFolders);
            _programs = new ListEditor("Add program...", PickProgram, config.AllowedPrograms);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(Page("Folders", "Partnerly may read, write, copy, zip, watch and use Git in these folders and everything inside them: files, subfolders and Git repositories. Everything else on this PC stays closed.", _folders));
            tabs.TabPages.Add(Page("Programs", "Partnerly may start, close and watch ONLY these programs. Add the program file (.exe). Leave empty to allow none.", _programs));
            tabs.TabPages.Add(SafetyPage());

            var save = new Button { Text = "Save and restart", Width = 150, Height = 34 };
            save.Click += (_, _) => Save();
            var cancel = new Button { Text = "Cancel", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 0) };
            bottom.Controls.Add(cancel);
            bottom.Controls.Add(save);
            CancelButton = cancel;

            Controls.Add(tabs);
            Controls.Add(bottom);
        }

        private static string? PickFolder()
        {
            using var dialog = new FolderBrowserDialog { UseDescriptionForTitle = true, Description = "Choose a folder", ShowNewFolderButton = true };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }

        private static string? PickProgram()
        {
            using var dialog = new OpenFileDialog { Title = "Choose a program", Filter = "Programs (*.exe)|*.exe", CheckFileExists = true };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        private static TabPage Page(string title, string explanation, Control body)
        {
            var page = new TabPage(title) { Padding = new Padding(10) };
            page.Controls.Add(body);
            page.Controls.Add(new Label { Text = explanation, Dock = DockStyle.Top, Height = 54 });
            return page;
        }

        private static bool IsChecked(AgentConfig config, string op) => op switch
        {
            GitPushLine => !config.GitPushWithoutAsking,
            GitRiskyLine => config.GitAskForRisky,
            _ => config.ConfirmOps.Contains(op, StringComparer.OrdinalIgnoreCase)
        };

        private TabPage SafetyPage()
        {
            foreach (var (op, caption) in Ask)
            {
                _confirm.Items.Add(caption, IsChecked(_config, op));
            }

            _clipboard.Checked = _config.WatchClipboard;
            _idle.Value = Math.Clamp(_config.IdleMinutes, 0, 600);
            _disk.Value = Math.Clamp(_config.DiskLowGb, 0, 5000);

            var page = new TabPage("Safety") { Padding = new Padding(10) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.Controls.Add(new Label { Text = "Ask me first (a window appears on this PC and Partnerly waits for your Yes) before:", Dock = DockStyle.Fill }, 0, 0);
            layout.Controls.Add(_confirm, 0, 1);
            layout.Controls.Add(_clipboard, 0, 2);
            layout.Controls.Add(Row("Tell Partnerly when nobody is at the PC for (minutes, 0 = never):", _idle), 0, 3);
            layout.Controls.Add(Row("Tell Partnerly when a drive has less free space than (GB, 0 = never):", _disk), 0, 4);
            page.Controls.Add(layout);
            return page;
        }

        private static Control Row(string text, Control input)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            row.Controls.Add(new Label { Text = text, AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
            row.Controls.Add(input);
            return row;
        }

        private void Save()
        {
            _config.AllowedFolders = _folders.Items;
            _config.AllowedPrograms = _programs.Items;
            _config.WatchClipboard = _clipboard.Checked;
            _config.IdleMinutes = (int)_idle.Value;
            _config.DiskLowGb = (int)_disk.Value;

            // Keep commands this window does not list (set by hand in the file) as they were.
            var known = Ask.Select(item => item.Op).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var confirm = _config.ConfirmOps.Where(op => !known.Contains(op)).ToList();
            for (var i = 0; i < Ask.Length; i++)
            {
                var ticked = _confirm.GetItemChecked(i);
                switch (Ask[i].Op)
                {
                    case GitPushLine: _config.GitPushWithoutAsking = !ticked; break;
                    case GitRiskyLine: _config.GitAskForRisky = ticked; break;
                    default:
                        if (ticked) { confirm.Add(Ask[i].Op); }
                        break;
                }
            }

            _config.ConfirmOps = confirm;
            _config.SettingsVersion = 2;
            try
            {
                _config.Save();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show("The settings could not be saved: " + ex.Message, "Partnerly Agent");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
