using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Authoring;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class TemplateCatalogTests
    {
        private readonly GraphValidator _validator = new();

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
        public void Every_template_graph_is_valid()
        {
            foreach (var preset in AutomationTemplateCatalog.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(preset.NameKey), preset.Id);
                Assert.False(string.IsNullOrWhiteSpace(preset.GraphJson), preset.Id);
                var errors = _validator.Validate(WorkflowGraph.Parse(preset.GraphJson));
                Assert.True(errors.Count == 0, preset.Id + ": " + string.Join(" ", errors));
            }
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
