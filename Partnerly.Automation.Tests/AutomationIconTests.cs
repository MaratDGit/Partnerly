using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Tests
{
    public class AutomationIconTests
    {
        [Theory]
        [InlineData("bx-rocket", "bx-rocket")]
        [InlineData("  bx-bell ", "bx-bell")]
        [InlineData("bx-nope\" onload=\"x", "bx-bot")]
        [InlineData("", "bx-bot")]
        [InlineData(null, "bx-bot")]
        public void Only_known_icons_are_accepted(string? input, string expected)
        {
            Assert.Equal(expected, AutomationIcons.Normalize(input));
        }

        [Fact]
        public void Icon_lives_in_the_note_and_defaults_when_empty()
        {
            var definition = new AutomationDefinition();
            Assert.Equal("bx-bot", definition.Icon);

            definition.Icon = "bxl-telegram";

            Assert.Equal("bxl-telegram", definition.Icon);
            Assert.Equal("icon:bxl-telegram", definition.Note);
        }

        [Fact]
        public async Task Saving_the_details_keeps_the_chosen_icon_and_ignores_unknown_ones()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var created = await authoring.CreateDraftAsync(owner, "icon");

            await authoring.UpdateMetaAsync(created.Id, "icon", null, "bx-rocket");
            Assert.Equal("bx-rocket", (await authoring.GetAsync(created.Id)).Icon);

            await authoring.UpdateMetaAsync(created.Id, "icon", null, null);
            Assert.Equal("bx-rocket", (await authoring.GetAsync(created.Id)).Icon);

            await authoring.UpdateMetaAsync(created.Id, "icon", null, "evil<script>");
            Assert.Equal("bx-bot", (await authoring.GetAsync(created.Id)).Icon);
        }
    }
}
