using JC.Web.ClientProfiling;
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
/// Assumes Bootstrap 5 is available.
/// </summary>
[HtmlTargetElement("bug-reporter", TagStructure = TagStructure.WithoutEndTag)]
public class BugReporterTagHelper : TagHelper
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
    /// The Bootstrap contextual suffix used for the card border, title, and submit button
    /// (e.g. <c>"danger"</c>, <c>"info"</c>, <c>"warning"</c>). The value is appended to
    /// <c>border-</c>, <c>text-</c>, and <c>btn-</c> classes, so custom values only work if
    /// matching utility classes exist (e.g. via SCSS). Defaults to <c>"danger"</c>.
    /// </summary>
    [HtmlAttributeName("colour")]
    public string Colour { get; set; } = "danger";

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
        var metadataLog = metadata?.ToLogEntry() ?? string.Empty;

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
        var escapedColour = System.Net.WebUtility.HtmlEncode(Colour);
        var escapedToken = antiforgeryToken != null
            ? System.Net.WebUtility.HtmlEncode(antiforgeryToken)
            : "";

        return $$"""
                <button id="br-{{id}}-toggle" type="button" class="d-print-none" title="{{escapedTitle}}"
                        aria-label="{{escapedTitle}}"
                        style="position:fixed;bottom:20px;right:20px;z-index:9999;cursor:pointer;font-size:2rem;
                               background:#fff;border:none;border-radius:50%;width:50px;height:50px;display:flex;
                               align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(0,0,0,.25);">
                    {{escapedIcon}}
                </button>
                <div id="br-{{id}}-box" class="card border-{{escapedColour}} d-print-none"
                     style="display:none;position:fixed;bottom:80px;right:20px;z-index:9999;width:350px;
                            box-shadow:0 4px 16px rgba(0,0,0,.25);"
                     data-endpoint="{{escapedEndpoint}}"
                     data-metadata="{{escapedMetadata}}"
                     data-token="{{escapedToken}}">
                    <div class="card-body p-3">
                        <h5 class="card-title text-{{escapedColour}}">{{escapedTitle}}</h5>
                        <div class="mb-2">
                            <label for="br-{{id}}-type" class="form-label">Type</label>
                            <select id="br-{{id}}-type" class="form-select form-select-sm">
                                <option value="bug">Bug</option>
                                <option value="suggestion">Suggestion</option>
                            </select>
                        </div>
                        <div class="mb-2">
                            <label for="br-{{id}}-desc" class="form-label">Description</label>
                            <textarea id="br-{{id}}-desc" class="form-control form-control-sm" rows="5"
                                      placeholder="Describe the issue..."></textarea>
                        </div>
                        <div id="br-{{id}}-alert" class="d-none"></div>
                        <div class="d-flex justify-content-between">
                            <button id="br-{{id}}-cancel" type="button" class="btn btn-sm btn-outline-secondary">Cancel</button>
                            <button id="br-{{id}}-submit" type="button" class="btn btn-sm btn-{{escapedColour}}">Submit</button>
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
                        alertEl.className = 'alert alert-' + type + ' py-1 px-2 mb-2 small';
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
                            setTimeout(function() { box.style.display = 'none'; alertEl.className = 'd-none'; submit.disabled = false; }, 4000);
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
}
