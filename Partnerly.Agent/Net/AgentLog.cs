using Partnerly.Agent.Config;

namespace Partnerly.Agent.Net
{
    // A small log next to the settings. It never contains the token.
    public static class AgentLog
    {
        private const long MaxBytes = 1_000_000;
        private static readonly object Gate = new();

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(AgentConfig.Folder);
                    var info = new FileInfo(AgentConfig.LogPath);
                    if (info.Exists && info.Length > MaxBytes)
                    {
                        File.Move(AgentConfig.LogPath, AgentConfig.LogPath + ".old", true);
                    }

                    File.AppendAllText(AgentConfig.LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
                }
            }
            catch (IOException) { /* logging must never stop the agent */ }
            catch (UnauthorizedAccessException) { }
        }
    }
}
