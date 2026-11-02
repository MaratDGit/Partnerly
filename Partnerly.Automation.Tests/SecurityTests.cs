using Partnerly.Automation.Persistence.Entities;
using Partnerly.Automation.Security;
using Partnerly.Helpers;

namespace Partnerly.Automation.Tests
{
    public class SecurityTests
    {
        [Fact]
        public void Webhook_signature_roundtrip()
        {
            var body = "{\"ok\":true}";
            var signature = WebhookSignature.Compute("secret", body);
            Assert.True(WebhookSignature.Verify("secret", body, signature));
            Assert.True(WebhookSignature.Verify("secret", body, "sha256=" + signature));
            Assert.False(WebhookSignature.Verify("secret", body, "deadbeef"));
            Assert.False(WebhookSignature.Verify("other", body, signature));
        }

        [Theory]
        [InlineData("http://example.com/hook", true)]
        [InlineData("https://partnerly.test/a", true)]
        [InlineData("http://8.8.8.8/lookup", true)]
        [InlineData("http://localhost/x", false)]
        [InlineData("http://127.0.0.1/x", false)]
        [InlineData("http://[::1]/x", false)]
        [InlineData("http://0.0.0.0/x", false)]
        [InlineData("http://10.0.0.8/x", false)]
        [InlineData("http://10.255.255.255/x", false)]
        [InlineData("http://192.168.1.1/x", false)]
        [InlineData("http://172.16.0.1/x", false)]
        [InlineData("http://100.64.0.1/x", false)]
        [InlineData("http://169.254.169.254/latest", false)]
        [InlineData("http://metadata/latest", false)]
        [InlineData("http://metadata.google.internal/", false)]
        [InlineData("http://evil.localhost/x", false)]
        [InlineData("http://host.local/x", false)]
        [InlineData("http://svc.internal/x", false)]
        [InlineData("http://[fc00::1]/x", false)]
        [InlineData("ftp://example.com/file", false)]
        [InlineData("file:///etc/passwd", false)]
        public void Http_guard_rejects_private_urls(string url, bool allowed)
        {
            var guard = new AutomationHttpGuard();
            Assert.Equal(allowed, guard.IsAllowed(url));
            Assert.Equal(allowed, PublicHttpGuard.IsAllowed(url));
        }

        [Fact]
        public async Task Http_guard_async_rejects_loopback_and_private_literals()
        {
            Assert.False(await PublicHttpGuard.IsAllowedAsync("http://127.0.0.1/x"));
            Assert.False(await PublicHttpGuard.IsAllowedAsync("http://10.1.2.3/x"));
            Assert.False(await PublicHttpGuard.IsAllowedAsync("http://[::1]/x"));
            Assert.False(await PublicHttpGuard.IsAllowedAsync("http://169.254.169.254/latest"));
            Assert.False(await new AutomationHttpGuard().IsAllowedAsync("http://192.168.0.10/x"));
        }

        [Fact]
        public void Access_hides_other_users_automations()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var definition = new AutomationDefinition { Id = Guid.NewGuid(), OwnerUserId = owner };
            var ownerAccess = new AutomationAccessService(owner, canManage: false);
            var otherAccess = new AutomationAccessService(other, canManage: false);
            var adminAccess = new AutomationAccessService(other, canManage: true);

            Assert.NotNull(ownerAccess.VisibleOrNull(definition));
            Assert.Null(otherAccess.VisibleOrNull(definition));
            Assert.NotNull(adminAccess.VisibleOrNull(definition));
        }
    }
}
