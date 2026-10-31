using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Partnerly.Helpers
{
    [HtmlTargetElement("partnerly-tabs")]
    public class PartnerlyTabsTagHelper : TagHelper
    {
        public string? Id { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var tabs = new List<PartnerlyTabItem>();
            context.Items[typeof(List<PartnerlyTabItem>)] = tabs;
            await output.GetChildContentAsync();

            if (tabs.Count > 0 && !tabs.Any(tab => tab.Active))
            {
                tabs[0].Active = true;
            }

            output.TagName = "div";
            var existingClass = output.Attributes["class"]?.Value?.ToString();
            var classes = string.IsNullOrWhiteSpace(existingClass)
                ? "partnerly-tabs"
                : existingClass.Contains("partnerly-tabs")
                    ? existingClass
                    : "partnerly-tabs " + existingClass;
            output.Attributes.SetAttribute("class", classes);

            var tablistId = string.IsNullOrWhiteSpace(Id) ? "partnerlyTabs" : Id;
            var contentId = tablistId + "Content";
            var html = new StringBuilder();

            html.Append("<ul class=\"nav nav-tabs\" id=\"")
                .Append(Encode(tablistId))
                .Append("\" role=\"tablist\">");

            foreach (var tab in tabs)
            {
                var buttonId = tab.Id + "-tab";
                var paneId = tab.Id + "-tab-pane";
                html.Append("<li class=\"nav-item\" role=\"presentation\">")
                    .Append("<button type=\"button\" class=\"nav-link")
                    .Append(tab.Active ? " active" : "")
                    .Append("\" id=\"").Append(Encode(buttonId)).Append("\"")
                    .Append(" data-bs-toggle=\"tab\" data-bs-target=\"#").Append(Encode(paneId)).Append("\"")
                    .Append(" role=\"tab\" aria-controls=\"").Append(Encode(paneId)).Append("\"")
                    .Append(" aria-selected=\"").Append(tab.Active ? "true" : "false").Append("\">");

                if (!string.IsNullOrWhiteSpace(tab.Icon))
                {
                    html.Append("<i class=\"").Append(Encode(tab.Icon)).Append("\"></i>");
                }

                html.Append("<span>").Append(Encode(tab.Title)).Append("</span>")
                    .Append("</button></li>");
            }

            html.Append("</ul>");
            html.Append("<div class=\"tab-content partnerly-tab-content\" id=\"")
                .Append(Encode(contentId))
                .Append("\">");

            foreach (var tab in tabs)
            {
                var buttonId = tab.Id + "-tab";
                var paneId = tab.Id + "-tab-pane";
                html.Append("<div class=\"tab-pane fade")
                    .Append(tab.Active ? " show active" : "")
                    .Append("\" id=\"").Append(Encode(paneId)).Append("\"")
                    .Append(" role=\"tabpanel\" aria-labelledby=\"").Append(Encode(buttonId)).Append("\"")
                    .Append(" tabindex=\"0\">")
                    .Append("<div class=\"partnerly-tab-body\">")
                    .Append(tab.Html)
                    .Append("</div></div>");
            }

            html.Append("</div>");
            output.Content.SetHtmlContent(html.ToString());
        }

        private static string Encode(string? value) =>
            HtmlEncoder.Default.Encode(value ?? string.Empty);
    }

    [HtmlTargetElement("partnerly-tab", ParentTag = "partnerly-tabs")]
    public class PartnerlyTabTagHelper : TagHelper
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public bool Active { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var tabs = (List<PartnerlyTabItem>)context.Items[typeof(List<PartnerlyTabItem>)];
            var content = await output.GetChildContentAsync();
            tabs.Add(new PartnerlyTabItem
            {
                Id = string.IsNullOrWhiteSpace(Id) ? "tab" + (tabs.Count + 1) : Id,
                Title = Title,
                Icon = Icon,
                Active = Active,
                Html = content.GetContent()
            });
            output.SuppressOutput();
        }
    }

    internal sealed class PartnerlyTabItem
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public bool Active { get; set; }

        public string Html { get; set; } = string.Empty;
    }
}
