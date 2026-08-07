using System.Net;
using JC.Core.Models;
using JC.FileStorage.Models;
using JC.FileStorage.Services;
using JC.FileStorage.Web.Framework;
using JC.FileStorage.Web.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using HtmlHelper = JC.Web.UI.HTML.HtmlHelper;

namespace JC.FileStorage.Web.TagHelpers;

/// <summary>
/// Renders the upload constraints for a folder as help text — the accepted file types and the
/// maximum size — using the configured UI framework's classes.
/// </summary>
/// <remarks>
/// Reads the limits from <see cref="FolderRegistry"/>, resolving the folder's own values and falling
/// back to the registry defaults. It therefore shows exactly what the server enforces and cannot
/// drift from it. Place beneath a file input.
/// </remarks>
[HtmlTargetElement("upload-constraints", TagStructure = TagStructure.WithoutEndTag)]
public class UploadConstraintsTagHelper : TagHelper
{
    private readonly FolderRegistry _folderRegistry;
    private readonly HtmlHelper _html;
    private readonly IFileStorageFrameworkDictionary _dictionary;

    /// <summary>Gets or sets the folder name to show constraints for. Required.</summary>
    [HtmlAttributeName("folder")]
    public string Folder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the tenant owning the folder. Defaults to the current user's tenant, or the
    /// no-tenant scope when JC.Identity is not registered.
    /// </summary>
    [HtmlAttributeName("tenant-id")]
    public string? TenantId { get; set; }

    /// <summary>Gets or sets whether to show the accepted file types. Defaults to true.</summary>
    [HtmlAttributeName("show-types")]
    public bool ShowTypes { get; set; } = true;

    /// <summary>Gets or sets whether to show the maximum file size. Defaults to true.</summary>
    [HtmlAttributeName("show-size")]
    public bool ShowSize { get; set; } = true;

    /// <summary>Gets or sets the label before the accepted types. Defaults to "Accepted types".</summary>
    [HtmlAttributeName("types-label")]
    public string TypesLabel { get; set; } = "Accepted types";

    /// <summary>Gets or sets the label before the maximum size. Defaults to "Maximum size".</summary>
    [HtmlAttributeName("size-label")]
    public string SizeLabel { get; set; } = "Maximum size";

    /// <summary>
    /// Gets or sets the text shown when a folder accepts any type. Defaults to
    /// "Any type except executable files".
    /// </summary>
    [HtmlAttributeName("any-type-text")]
    public string AnyTypeText { get; set; } = "Any type except executable files";

    /// <summary>
    /// Gets or sets the CSS classes applied to the wrapper. Falls back to the configured framework's
    /// container class when null or whitespace.
    /// </summary>
    [HtmlAttributeName("css-class")]
    public string? CssClass { get; set; }

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    /// <param name="folderRegistry">The registry supplying the folder's upload limits.</param>
    /// <param name="html">The HTML element builder, resolved from the container.</param>
    /// <param name="dictionary">The class dictionary for the configured framework.</param>
    public UploadConstraintsTagHelper(FolderRegistry folderRegistry,
        HtmlHelper html,
        IFileStorageFrameworkDictionary dictionary)
    {
        _folderRegistry = folderRegistry;
        _html = html;
        _dictionary = dictionary;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Folder))
            throw new InvalidOperationException(
                "The 'folder' attribute is required on <upload-constraints>.");

        //Optional, like StorageService - no JC.Identity means the no-tenant scope
        var tenantId = TenantId
            ?? ViewContext.HttpContext.RequestServices.GetService<IUserInfo>()?.TenantId;

        if (!_folderRegistry.TryGetFolder(Folder, tenantId, out var folder) || folder == null)
            throw new InvalidOperationException(
                $"Folder '{Folder}' is not registered for tenant '{tenantId ?? FolderModel.NullTenantName}'. " +
                "Register it with AddFolders before rendering <upload-constraints>.");

        var parts = BuildParts(folder);
        if (parts.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        var css = string.IsNullOrWhiteSpace(CssClass)
            ? _dictionary.UploadConstraints.Container
            : CssClass;

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(_html.CreateElement("div",
            string.Join(" &middot; ", parts),
            classes: WebUtility.HtmlEncode(css)));
    }

    private List<string> BuildParts(FolderModel folder)
    {
        var parts = new List<string>();

        if (ShowTypes)
        {
            //Null means the folder and the registry both leave it open, so the blocked list is the
            //only type rule in force - say so rather than implying anything goes
            var allowed = _folderRegistry.ResolveAllowedExtensions(folder);
            var text = allowed == null
                ? WebUtility.HtmlEncode(AnyTypeText)
                : $"{WebUtility.HtmlEncode(TypesLabel)}: {WebUtility.HtmlEncode(string.Join(", ", allowed))}";

            parts.Add(text);
        }

        if (ShowSize)
        {
            var maxBytes = _folderRegistry.ResolveMaxBytes(folder);
            if (maxBytes != null)
                parts.Add($"{WebUtility.HtmlEncode(SizeLabel)}: " +
                          $"{WebUtility.HtmlEncode(FormFileHelper.FormatFileSize(maxBytes.Value))}");
        }

        return parts;
    }
}