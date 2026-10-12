using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace EventTixHub.Web.TagHelpers;

// Links a server-side field error to its field, so it holds with JS off: the field gets aria-invalid="true" and
// aria-describedby="{id}-error", and the message inside the validation span gets that id. "{id}-error" is the id
// jquery.validate gives the message it writes, so the same link stays valid once client validation takes over.
// The id comes from the field name on both sides, since the span cannot see the input: a view that sets its own
// id on an asp-for field keeps the server link, and jquery.validate adds its own "{custom id}-error" link.
[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "asp-for")]
[HtmlTargetElement("span", Attributes = "asp-validation-for")]
public class FieldErrorTagHelper(IHtmlGenerator generator) : TagHelper
{
    // After the MVC input and validation-message tag helpers, which set the id and the message.
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")] public ModelExpression? For { get; set; }
    [HtmlAttributeName("asp-validation-for")] public ModelExpression? ValidationFor { get; set; }
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var name = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName((For ?? ValidationFor)!.Name);
        if (ViewContext.ViewData.ModelState.GetFieldValidationState(name) != ModelValidationState.Invalid) return;

        var errorId = TagBuilder.CreateSanitizedId(name, generator.IdAttributeDotReplacement) + "-error";
        if (output.TagName == "span")
        {
            output.PreContent.SetHtmlContent($"<span id=\"{errorId}\">");
            output.PostContent.SetHtmlContent("</span>");
            return;
        }
        var describedBy = output.Attributes["aria-describedby"]?.Value?.ToString();
        output.Attributes.SetAttribute("aria-invalid", "true");
        output.Attributes.SetAttribute("aria-describedby", string.IsNullOrEmpty(describedBy) ? errorId : $"{describedBy} {errorId}");
    }
}
