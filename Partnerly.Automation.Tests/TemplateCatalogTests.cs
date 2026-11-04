using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Authoring;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class TemplateCatalogTests
    {
        [Fact]
        public void Catalog_includes_small_medium_and_large()
        {
            Assert.True(AutomationTemplateCatalog.All.Count >= 8);
            Assert.Contains(AutomationTemplateCatalog.All, item => item.Size == "small");
            Assert.Contains(AutomationTemplateCatalog.All, item => item.Size == "medium");
            Assert.Contains(AutomationTemplateCatalog.All, item => item.Size == "large");
            Assert.NotNull(AutomationTemplateCatalog.Get("manual-flag"));
            Assert.NotNull(AutomationTemplateCatalog.Get("ticket-triage"));
            Assert.NotNull(AutomationTemplateCatalog.Get("new-user-onboarding"));
        }

        [Fact]
        public async Task Every_template_graph_is_valid()
        {
            // Integration templates need the registry-backed validator.
            await using var provider = Integrations.IntegrationTestHost.Create(Guid.NewGuid(), new Integrations.FakeHttpHandler());
            var validator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GraphValidator>(provider);
            foreach (var preset in AutomationTemplateCatalog.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(preset.NameKey), preset.Id);
                Assert.False(string.IsNullOrWhiteSpace(preset.GraphJson), preset.Id);
                // Integration templates leave connectionId for the user to pick in the builder.
                var errors = validator.Validate(WorkflowGraph.Parse(preset.GraphJson))
                    .Where(error => !error.Contains("connection.", StringComparison.Ordinal))
                    .ToList();
                Assert.True(errors.Count == 0, preset.Id + ": " + string.Join(" ", errors));
            }
        }

        [Fact]
        public void Integration_templates_carry_services_and_category()
        {
            var omni = AutomationTemplateCatalog.Get("omnichannel-ticket")!;
            Assert.Equal("telegram", omni.Category);
            Assert.Equal(new[] { "Telegram", "Google Sheets", "Gmail", "WhatsApp" }, omni.Services);
            Assert.Equal("google", AutomationTemplateCatalog.Get("sheets-signup-log")!.Category);
            Assert.Equal("whatsapp", AutomationTemplateCatalog.Get("whatsapp-auto-reply")!.Category);
            Assert.Equal("partnerly", AutomationTemplateCatalog.Get("manual-flag")!.Category);
            Assert.Empty(AutomationTemplateCatalog.Get("new-ticket-alert")!.Services);
        }

        [Fact]
        public void Large_templates_have_branch_and_filled_fields()
        {
            var onboarding = AutomationTemplateCatalog.Get("new-user-onboarding")!;
            Assert.Contains("UserRegistered", onboarding.GraphJson, StringComparison.Ordinal);
            Assert.Contains("send_email", onboarding.GraphJson, StringComparison.Ordinal);
            Assert.Contains("create_record", onboarding.GraphJson, StringComparison.Ordinal);
            Assert.Contains("\"port\":\"true\"", onboarding.GraphJson, StringComparison.Ordinal);

            var triage = AutomationTemplateCatalog.Get("ticket-triage")!;
            Assert.Contains("http_request", triage.GraphJson, StringComparison.Ordinal);
            Assert.Contains("find_record", triage.GraphJson, StringComparison.Ordinal);
            Assert.Contains("notify@example.com", triage.GraphJson, StringComparison.Ordinal);
            Assert.Contains("wait_until", triage.GraphJson, StringComparison.Ordinal);
        }
    }
}
