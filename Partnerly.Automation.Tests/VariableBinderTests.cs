using System.Text.Json.Nodes;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class VariableBinderTests
    {
        private readonly VariableBinder _binder = new();

        [Fact]
        public void Interpolates_known_paths()
        {
            var ctx = new JsonObject
            {
                ["trigger"] = new JsonObject
                {
                    ["user"] = new JsonObject { ["email"] = "a@b.c" }
                },
                ["execution"] = new JsonObject { ["runId"] = "r1" }
            };

            var text = _binder.Interpolate("hi {{trigger.user.email}} {{execution.runId}}", ctx);
            Assert.Equal("hi a@b.c r1", text);
        }

        [Fact]
        public void Blocks_password_hash()
        {
            var ctx = new JsonObject
            {
                ["user"] = new JsonObject { ["passwordHash"] = "secret", ["email"] = "ok@x" }
            };

            Assert.True(_binder.IsBlockedPath("user.passwordHash"));
            Assert.Equal(string.Empty, _binder.Interpolate("{{user.passwordHash}}", ctx));
            Assert.Equal("ok@x", _binder.Interpolate("{{user.email}}", ctx));
        }

        [Theory]
        [InlineData("user.password")]
        [InlineData("user.apiKey")]
        [InlineData("user.token")]
        [InlineData("smtp.smtpPassword")]
        [InlineData("oauth.clientSecret")]
        [InlineData("keys.privateKey")]
        [InlineData("google.googleId")]
        [InlineData("apple.appleId")]
        [InlineData("db.connectionString")]
        [InlineData("nested.profile.secret")]
        public void Blocks_denylist_segments(string path)
        {
            Assert.True(_binder.IsBlockedPath(path));
            var root = new JsonObject();
            JsonObject current = root;
            var parts = path.Split('.');
            for (var i = 0; i < parts.Length - 1; i++)
            {
                var next = new JsonObject();
                current[parts[i]] = next;
                current = next;
            }

            current[parts[^1]] = "leaked";
            Assert.Equal(string.Empty, _binder.Interpolate("{{" + path + "}}", root));
        }
    }
}
