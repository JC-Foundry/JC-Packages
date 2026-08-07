using JC.Web.ClientProfiling;
using JC.Web.UI.Framework;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Web.UI.TagHelpers;

/// <summary>
/// Tag helper that renders a floating bug reporter widget with a toggle button,
/// a report form (type + description), and JavaScript to submit reports via POST.
/// Automatically includes <see cref="ClientProfiling.Models.RequestMetadata"/> as context
/// in the submission payload, and sends an anti-forgery token when available.
/// <para>
/// Usage: <c>&lt;bug-reporter endpoint="/Bug/ReportBug" /&gt;</c>
/// </para>
/// <para>
/// The consuming application must provide the POST endpoint. The widget sends a JSON body:
/// <code>{ "type": "bug"|"suggestion", "description": "...", "metadata": "..." }</code>
/// </para>
/// Renders using the configured UI framework's classes.
/// </summary>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
[HtmlTargetElement("bug-reporter", TagStructure = TagStructure.WithoutEndTag)]
public class BugReporterTagHelper(IWebFrameworkDictionary dictionary) : TagHelper
{
    /// <summary>
    /// The POST endpoint that receives bug reports. Required.
    /// </summary>
    /// <example><c>/Bug/ReportBug</c></example>
    [HtmlAttributeName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// The icon displayed on the floating button. Defaults to the bug emoji.
    /// </summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; } = "🐞";

    /// <summary>
    /// The title text for the report form. Defaults to <c>"Send Feedback"</c>.
    /// </summary>
    [HtmlAttributeName("title")]
    public string Title { get; set; } = "Send Feedback";

    /// <summary>
    /// The contextual colour used for the panel border, title, and submit button
    /// (e.g. <c>"danger"</c>, <c>"info"</c>, <c>"warning"</c>). Falls back to the configured
    /// framework's default when unset.
    /// </summary>
    /// <remarks>
    /// How the value becomes a class is the dictionary's business, not this tag helper's — under
    /// Bootstrap it fills the <c>border-</c>, <c>text-</c> and <c>btn-</c> formats on
    /// <see cref="BugReporterClasses"/>, so custom values only work where matching utility classes
    /// exist. The default lives on the dictionary for the same reason: <c>"danger"</c> is a
    /// Bootstrap colour name, not a universal one.
    /// </remarks>
    [HtmlAttributeName("colour")]
    public string? Colour { get; set; }
    
    public bool MaskRequestPath { get; set; } = false;
    public bool MaskQuery { get; set; } = true;

    /// <summary>
    /// The ViewContext, automatically injected by the framework.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException(
                "The 'endpoint' attribute is required on <bug-reporter>. " +
                "Specify the POST route that handles bug report submissions.");

        var httpContext = ViewContext.HttpContext;
        var metadata = httpContext.GetRequestMetadata();
        var metadataLog = metadata?.ToLogEntry(maskPath: MaskRequestPath, maskQuery: MaskQuery) ?? string.Empty;

        // Get anti-forgery token if available
        var antiforgery = httpContext.RequestServices.GetService<IAntiforgery>();
        string? antiforgeryToken = null;
        if (antiforgery != null)
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            antiforgeryToken = tokens.RequestToken;
        }

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml(metadataLog, antiforgeryToken));
    }

    private string BuildHtml(string metadataLog, string? antiforgeryToken)
    {
        var id = Guid.NewGuid().ToString("N")[..8];

        var escapedMetadata = System.Net.WebUtility.HtmlEncode(metadataLog);
        var escapedEndpoint = System.Net.WebUtility.HtmlEncode(Endpoint);
        var escapedTitle = System.Net.WebUtility.HtmlEncode(Title);
        var escapedIcon = System.Net.WebUtility.HtmlEncode(Icon);
        var escapedToken = antiforgeryToken != null
            ? System.Net.WebUtility.HtmlEncode(antiforgeryToken)
            : "";

        var css = dictionary.BugReporter;

        var escapedColour = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(Colour) ? css.DefaultColour : Colour);

        // The outcome is not known until the request completes, so the feedback class is substituted
        // in the browser. The format is the dictionary's, not this helper's.
        var feedbackFormat = JavaScriptString(css.FeedbackFormat);
        var hiddenClass = JavaScriptString(css.Hidden);

        return $$"""
                <button id="br-{{id}}-toggle" type="button" class="{{css.ToggleButton}}" title="{{escapedTitle}}"
                        aria-label="{{escapedTitle}}"
                        style="position:fixed;bottom:20px;right:20px;z-index:9999;cursor:pointer;font-size:2rem;
                               background:#fff;border:none;border-radius:50%;width:50px;height:50px;display:flex;
                               align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(0,0,0,.25);">
                    {{escapedIcon}}
                </button>
                <div id="br-{{id}}-box" class="{{css.Panel(escapedColour)}}"
                     style="display:none;position:fixed;bottom:80px;right:20px;z-index:9999;width:350px;
                            box-shadow:0 4px 16px rgba(0,0,0,.25);"
                     data-endpoint="{{escapedEndpoint}}"
                     data-metadata="{{escapedMetadata}}"
                     data-token="{{escapedToken}}">
                    <div class="{{css.PanelBody}}">
                        <h5 class="{{css.Title(escapedColour)}}">{{escapedTitle}}</h5>
                        <div class="{{css.Field}}">
                            <label for="br-{{id}}-type" class="{{css.Label}}">Type</label>
                            <select id="br-{{id}}-type" class="{{css.Select}}">
                                <option value="bug">Bug</option>
                                <option value="suggestion">Suggestion</option>
                            </select>
                        </div>
                        <div class="{{css.Field}}">
                            <label for="br-{{id}}-desc" class="{{css.Label}}">Description</label>
                            <textarea id="br-{{id}}-desc" class="{{css.TextArea}}" rows="5"
                                      placeholder="Describe the issue..."></textarea>
                        </div>
                        <div id="br-{{id}}-alert" class="{{css.Hidden}}"></div>
                        <div class="{{css.Actions}}">
                            <button id="br-{{id}}-cancel" type="button" class="{{css.CancelButton}}">Cancel</button>
                            <button id="br-{{id}}-submit" type="button" class="{{css.SubmitButton(escapedColour)}}">Submit</button>
                        </div>
                    </div>
                </div>
                <script>
                (function() {
                    var p = 'br-{{id}}';
                    var toggle = document.getElementById(p + '-toggle');
                    var box = document.getElementById(p + '-box');
                    var cancel = document.getElementById(p + '-cancel');
                    var submit = document.getElementById(p + '-submit');
                    var desc = document.getElementById(p + '-desc');
                    var alertEl = document.getElementById(p + '-alert');

                    function showAlert(msg, type) {
                        alertEl.className = '{{feedbackFormat}}'.replace('{0}', type);
                        alertEl.textContent = msg;
                    }

                    toggle.addEventListener('click', function() {
                        box.style.display = box.style.display === 'block' ? 'none' : 'block';
                    });

                    cancel.addEventListener('click', function() {
                        box.style.display = 'none';
                    });

                    submit.addEventListener('click', function() {
                        var type = document.getElementById(p + '-type').value;
                        var text = desc.value;
                        if (!text.trim()) { showAlert('Please enter a description.', 'warning'); return; }

                        submit.disabled = true;

                        var headers = { 'Content-Type': 'application/json' };
                        var token = box.getAttribute('data-token');
                        if (token) headers['RequestVerificationToken'] = token;

                        fetch(box.getAttribute('data-endpoint'), {
                            method: 'POST',
                            headers: headers,
                            body: JSON.stringify({
                                type: type,
                                description: text,
                                metadata: box.getAttribute('data-metadata')
                            })
                        })
                        .then(function(r) {
                            if (!r.ok) throw new Error('Failed');
                            showAlert('Thank you for your feedback!', 'success');
                            desc.value = '';
                            setTimeout(function() { box.style.display = 'none'; alertEl.className = '{{hiddenClass}}'; submit.disabled = false; }, 4000);
                        })
                        .catch(function() {
                            showAlert('Something went wrong. Please try again.', 'danger');
                            submit.disabled = false;
                        });
                    });
                })();
                </script>
                """;
    }

    /// <summary>
    /// Escapes a class value for use inside a single-quoted JavaScript string literal.
    /// </summary>
    /// <remarks>
    /// Dictionary values are written by whoever implements the dictionary rather than supplied by a
    /// request, so this guards against an awkward class name breaking the script rather than against
    /// an attacker.
    /// </remarks>
    private static string JavaScriptString(string value)
        => value.Replace("\\", "\\\\").Replace("'", "\\'");
}
