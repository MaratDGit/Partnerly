using System.Drawing;
using System.Drawing.Imaging;

namespace Partnerly.Agent.UI
{
    // The few things that need the screen: balloons, windows, clipboard, screenshots.
    public sealed class UiBridge
    {
        private readonly NotifyIcon _tray;

        public UiBridge(NotifyIcon tray)
        {
            _tray = tray;
        }

        public void Notify(string title, string text)
        {
            if (_tray.ContextMenuStrip?.IsHandleCreated == true)
            {
                _tray.ContextMenuStrip.Invoke(() => _tray.ShowBalloonTip(8000, Clip(title, 60, "Partnerly"), Clip(text, 240, " "), ToolTipIcon.Info));
            }
            else
            {
                _tray.ShowBalloonTip(8000, Clip(title, 60, "Partnerly"), Clip(text, 240, " "), ToolTipIcon.Info);
            }
        }

        // Shows a window and returns the button the user pressed.
        public string MessageBox(string title, string text, bool yesNo) =>
            RunSta(() =>
            {
                var result = System.Windows.Forms.MessageBox.Show(
                    Clip(text, 1000, " "),
                    Clip(title, 80, "Partnerly"),
                    yesNo ? MessageBoxButtons.YesNo : MessageBoxButtons.OK,
                    yesNo ? MessageBoxIcon.Question : MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);
                return result.ToString();
            });

        // Asks the person at this PC before a command the settings marked as "confirm".
        public bool Confirm(string text) =>
            RunSta(() => System.Windows.Forms.MessageBox.Show(
                Clip(text, 600, " "), "Partnerly Agent", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);

        public void SetClipboard(string text) =>
            RunSta(() =>
            {
                if (string.IsNullOrEmpty(text)) { Clipboard.Clear(); } else { Clipboard.SetText(text); }
                return true;
            });

        public string GetClipboard() =>
            RunSta(() => Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty);

        // PNG of the main screen or of all screens.
        public byte[] Screenshot(bool allScreens)
        {
            var bounds = allScreens ? SystemInformation.VirtualScreen : (Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        // Clipboard and windows must live on an STA thread.
        private static T RunSta<T>(Func<T> work)
        {
            T result = default!;
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try { result = work(); }
                catch (Exception ex) { error = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (error != null)
            {
                throw error;
            }

            return result;
        }

        private static string Clip(string? text, int max, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(text) ? fallback : text;
            return value.Length > max ? value[..max] : value;
        }
    }
}
