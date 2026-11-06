using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Partnerly.Agent.UI
{
    public enum AgentState
    {
        Connecting,
        Online,
        Offline,
        Paused,
        Problem
    }

    // The program icon (embedded app.ico) with a small coloured dot in the corner that tells the state.
    public static class TrayIcons
    {
        private static readonly Dictionary<AgentState, Icon> Cache = new();

        public static Icon App()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
            return stream == null ? SystemIcons.Application : new Icon(stream, new Size(32, 32));
        }

        public static Icon For(AgentState state)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(state, out var cached))
                {
                    return cached;
                }

                Icon icon;
                try
                {
                    using var baseIcon = App();
                    using var bitmap = baseIcon.ToBitmap();
                    using var canvas = new Bitmap(32, 32);
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawImage(bitmap, 0, 0, 32, 32);
                        var color = state switch
                        {
                            AgentState.Online => Color.FromArgb(34, 197, 94),
                            AgentState.Connecting => Color.FromArgb(245, 158, 11),
                            AgentState.Problem => Color.FromArgb(239, 68, 68),
                            _ => Color.FromArgb(148, 163, 184)
                        };
                        using var ring = new SolidBrush(Color.White);
                        using var fill = new SolidBrush(color);
                        g.FillEllipse(ring, 16, 16, 16, 16);
                        g.FillEllipse(fill, 19, 19, 10, 10);
                    }

                    // The handle is owned by the cache for the whole run of the program.
                    icon = Icon.FromHandle(canvas.GetHicon());
                }
                catch (Exception ex) when (ex is ExternalException or ArgumentException)
                {
                    icon = SystemIcons.Application;
                }

                Cache[state] = icon;
                return icon;
            }
        }
    }
}
