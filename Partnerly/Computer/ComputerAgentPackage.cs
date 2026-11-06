using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Partnerly.Computer
{
    // The download of Partnerly Agent: the program itself (published into the download folder
    // by tools/publish-agent.ps1) and, when the user has just paired a computer, a config.json
    // with the site address and token, so nothing has to be typed on the PC.
    public static class ComputerAgentPackage
    {
        public const string ExeName = "Partnerly.Agent.exe";

        public static string DefaultFolder(string contentRoot) => Path.Combine(contentRoot, "App_Data", "agent");

        public static string? FindExe(string folder)
        {
            var path = Path.Combine(folder, ExeName);
            return File.Exists(path) ? path : null;
        }

        public static byte[] Build(string exePath, string? serverUrl, string? token)
        {
            using var output = new MemoryStream();
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                var exe = zip.CreateEntry(ExeName, CompressionLevel.Optimal);
                using (var entry = exe.Open())
                using (var file = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    file.CopyTo(entry);
                }

                if (!string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(token))
                {
                    var config = zip.CreateEntry("config.json", CompressionLevel.Optimal);
                    using var writer = new StreamWriter(config.Open(), new UTF8Encoding(false));
                    writer.Write(JsonSerializer.Serialize(new { server = serverUrl.TrimEnd('/'), token }, new JsonSerializerOptions { WriteIndented = true }));
                }

                var readme = zip.CreateEntry("README.txt", CompressionLevel.Optimal);
                using var text = new StreamWriter(readme.Open(), new UTF8Encoding(false));
                text.Write(
                    "Partnerly Agent\r\n\r\n" +
                    "1. Right-click the zip and choose Extract All (do not start the program from inside the zip), then start Partnerly.Agent.exe.\r\n" +
                    "2. The program offers to install itself for the current Windows user (no administrator rights needed).\r\n" +
                    (string.IsNullOrEmpty(token)
                        ? "3. Enter the site address and the token shown on the Integrations page.\r\n"
                        : "3. The site address and token are already in config.json next to the program. Keep that file private.\r\n") +
                    "\r\nWhat Partnerly may do on this computer (folders, programs, watching) is chosen in the program: tray icon > Settings.\r\nTo remove it: Windows Settings > Apps, or tray icon > Remove program.\r\n");
            }

            return output.ToArray();
        }
    }
}
