using System.IO.Compression;
using System.Text.Json.Nodes;
using Partnerly.Computer;

namespace Partnerly.Automation.Tests.Integrations
{
    public class ComputerAgentPackageTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "agentpkg-" + Guid.NewGuid().ToString("N"));

        public ComputerAgentPackageTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }

        private static Dictionary<string, string> Read(byte[] zip)
        {
            using var archive = new ZipArchive(new MemoryStream(zip));
            return archive.Entries.ToDictionary(item => item.FullName, item =>
            {
                using var reader = new StreamReader(item.Open());
                return reader.ReadToEnd();
            });
        }

        [Fact]
        public void Missing_program_is_reported_as_not_available()
        {
            Assert.Null(ComputerAgentPackage.FindExe(_folder));
            File.WriteAllText(Path.Combine(_folder, ComputerAgentPackage.ExeName), "exe");
            Assert.NotNull(ComputerAgentPackage.FindExe(_folder));
        }

        [Fact]
        public void Plain_download_has_the_program_and_no_secrets()
        {
            var exe = Path.Combine(_folder, ComputerAgentPackage.ExeName);
            File.WriteAllText(exe, "MZ-fake");

            var files = Read(ComputerAgentPackage.Build(exe, null, null));

            Assert.Equal("MZ-fake", files[ComputerAgentPackage.ExeName]);
            Assert.DoesNotContain("config.json", files.Keys);
            Assert.Contains("README.txt", files.Keys);
        }

        [Fact]
        public void Download_with_settings_carries_the_address_and_token()
        {
            var exe = Path.Combine(_folder, ComputerAgentPackage.ExeName);
            File.WriteAllText(exe, "MZ-fake");

            var files = Read(ComputerAgentPackage.Build(exe, "https://site.example/", "pc.abc.def"));

            var config = JsonNode.Parse(files["config.json"])!;
            Assert.Equal("https://site.example", config["server"]!.ToString());
            Assert.Equal("pc.abc.def", config["token"]!.ToString());
            Assert.Contains("config.json", files["README.txt"]);
        }
    }
}
