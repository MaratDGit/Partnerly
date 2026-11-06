using System.Runtime.InteropServices;
using System.Text;

namespace Partnerly.Agent.Commands
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo info);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int max);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool PostMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref ShFileOpStruct operation);

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint Size;
            public uint Time;
        }

        // The 64-bit layout (the program is published for win-x64).
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShFileOpStruct
        {
            public IntPtr Window;
            public uint Function;
            [MarshalAs(UnmanagedType.LPWStr)] public string From;
            [MarshalAs(UnmanagedType.LPWStr)] public string? To;
            public ushort Flags;
            public bool AnyOperationsAborted;
            public IntPtr NameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string? Title;
        }

        private const uint WmSysCommand = 0x0112;
        private const int ScMonitorPower = 0xF170;
        private static readonly IntPtr HwndBroadcast = new(0xFFFF);

        // Minutes since the last key press or mouse move.
        public static double IdleMinutes()
        {
            var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
            return GetLastInputInfo(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.Time)).TotalMinutes : 0;
        }

        public static (string Title, int ProcessId) ActiveWindow()
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return (string.Empty, 0);
            }

            var text = new StringBuilder(256);
            GetWindowText(window, text, text.Capacity);
            GetWindowThreadProcessId(window, out var processId);
            return (text.ToString(), (int)processId);
        }

        // Presses a media key: 0xAD mute, 0xAE quieter, 0xAF louder, 0xB0 next, 0xB1 previous, 0xB2 stop, 0xB3 play/pause.
        public static void PressMediaKey(byte key)
        {
            keybd_event(key, 0, 0, UIntPtr.Zero);
            keybd_event(key, 0, 2, UIntPtr.Zero);
        }

        public static void MonitorOff()
        {
            PostMessageW(HwndBroadcast, WmSysCommand, (IntPtr)ScMonitorPower, (IntPtr)2);
        }

        // Moves a file to the Recycle Bin (it can be restored). Returns false when Windows refused.
        public static bool MoveToRecycleBin(string path)
        {
            var operation = new ShFileOpStruct
            {
                Function = 3,           // FO_DELETE
                From = path + "\0",
                Flags = 0x0040 | 0x0010 | 0x0004 | 0x0400  // ALLOWUNDO | NOCONFIRMATION | SILENT | NOERRORUI
            };
            return SHFileOperation(ref operation) == 0 && !operation.AnyOperationsAborted;
        }
    }
}
