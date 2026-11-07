using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Partnerly;

namespace Partnerly.Automation.Tests
{
    // The search in the editor's card list needs every card name in English, Russian and Armenian.
    public class NodeNameLocalizationTests
    {
        private static IStringLocalizer<SharedResource> Localizer()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
        }

        private static string In(IStringLocalizer<SharedResource> localizer, string culture, string key)
        {
            var saved = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                return localizer[key].Value;
            }
            finally
            {
                CultureInfo.CurrentUICulture = saved;
            }
        }

        [Theory]
        [InlineData("NodeTypeSendEmail")]
        [InlineData("NodeTypeGitHubCreateIssue")]
        [InlineData("NodeTypeComputerGitCommit")]
        [InlineData("NodeTypeTelegramSendMessage")]
        public void A_card_name_differs_in_each_language(string key)
        {
            var localizer = Localizer();

            var english = In(localizer, "en", key);
            var russian = In(localizer, "ru", key);
            var armenian = In(localizer, "hy", key);

            Assert.NotEqual(key, english);
            Assert.NotEqual(english, russian);
            Assert.NotEqual(english, armenian);
            Assert.NotEqual(russian, armenian);
        }

        [Fact]
        public void Every_node_type_name_exists_in_all_three_resource_files()
        {
            var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            // The tests run from a temporary folder sometimes; find the repository by walking up.
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Partnerly", "Resources", "SharedResource.en.resx")))
            {
                folder = folder.Parent;
            }

            if (folder == null)
            {
                return;
            }

            var names = new Dictionary<string, HashSet<string>>();
            foreach (var language in new[] { "en", "ru", "hy" })
            {
                var text = File.ReadAllText(Path.Combine(folder.FullName, "Partnerly", "Resources", $"SharedResource.{language}.resx"));
                names[language] = System.Text.RegularExpressions.Regex.Matches(text, "<data name=\"(NodeType[A-Za-z0-9]+)\"")
                    .Select(match => match.Groups[1].Value).ToHashSet();
            }

            var missing = names["en"].SelectMany(key => new[] { "ru", "hy" }.Where(language => !names[language].Contains(key)).Select(language => language + ":" + key)).ToList();

            Assert.Empty(missing);
        }
    }
}
