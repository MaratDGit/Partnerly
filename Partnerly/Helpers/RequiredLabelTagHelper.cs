using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Partnerly.Helpers
{
    [HtmlTargetElement("label", Attributes = "asp-for")]
    public class RequiredLabelTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; } = default!;

        public override void Process(
            TagHelperContext context,
            TagHelperOutput output)
        {
            var isRequired = For.Metadata.ValidatorMetadata
                .OfType<RequiredAttribute>()
                .Any();

            if (!isRequired)
                return;

            var existingClass = output.Attributes["class"]?.Value?.ToString();

            var classes = string.IsNullOrWhiteSpace(existingClass)
                ? "required"
                : $"{existingClass} required";

            output.Attributes.SetAttribute("class", classes);
        }
    }
}