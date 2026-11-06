using Partnerly.Agent.Security;

namespace Partnerly.Automation.Tests.Integrations
{
    // PathGuard is compiled into this project straight from Partnerly.Agent (it has no Windows-only parts).
    public class AgentPathGuardTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "pg-" + Guid.NewGuid().ToString("N"));
        private readonly PathGuard _guard;

        public AgentPathGuardTests()
        {
            Directory.CreateDirectory(Path.Combine(_root, "allowed", "sub"));
            Directory.CreateDirectory(Path.Combine(_root, "allowed-evil"));
            _guard = new PathGuard(new[] { Path.Combine(_root, "allowed") });
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch (IOException) { }
        }

        [Fact]
        public void Allowed_folder_and_everything_below_it_pass()
        {
            var file = Path.Combine(_root, "allowed", "sub", "a.txt");
            Assert.Equal(file, _guard.Resolve(file));
            Assert.Equal(Path.Combine(_root, "allowed"), _guard.Resolve(Path.Combine(_root, "allowed")));
        }

        [Fact]
        public void Places_outside_are_refused_even_when_the_name_starts_the_same()
        {
            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(_root, "allowed-evil", "a.txt")));
            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(_root, "other.txt")));
            Assert.Throws<AgentException>(() => _guard.Resolve(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
        }

        [Fact]
        public void Dot_dot_cannot_climb_out()
        {
            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(_root, "allowed", "..", "other.txt")));
            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(_root, "allowed", "sub", "..", "..", "allowed-evil", "a.txt")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("relative/file.txt")]
        public void Empty_and_relative_paths_are_refused(string path) =>
            Assert.Throws<AgentException>(() => _guard.Resolve(path));

        [Fact]
        public void Device_paths_and_data_streams_are_refused()
        {
            var slashes = new string((char)92, 2);
            Assert.Throws<AgentException>(() => _guard.Resolve(slashes + "?" + (char)92 + "C:" + (char)92 + "x"));
            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(_root, "allowed", "a.txt") + ":hidden"));
        }

        [Fact]
        public void Nothing_is_allowed_when_no_folder_is_listed()
        {
            var empty = new PathGuard(Array.Empty<string>());
            Assert.Throws<AgentException>(() => empty.Resolve(Path.Combine(_root, "allowed", "a.txt")));
        }

        [Fact]
        public void A_junction_inside_an_allowed_folder_is_not_followed()
        {
            var target = Path.Combine(_root, "allowed-evil");
            var link = Path.Combine(_root, "allowed", "link");
            try
            {
                Directory.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return; // creating links needs a privilege on this PC; nothing to check then
            }

            Assert.Throws<AgentException>(() => _guard.Resolve(Path.Combine(link, "a.txt")));
        }
    }
}
