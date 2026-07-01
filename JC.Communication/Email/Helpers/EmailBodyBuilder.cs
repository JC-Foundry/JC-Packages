using System.Net;
using System.Text;
using JC.Communication.Email.Models;

namespace JC.Communication.Email.Helpers;

public sealed class EmailBodyBuilder
{
    private readonly string _caption;
    private readonly EmailBranding _branding;
    
    private readonly List<(string Plain, string Html)> _sections = [];
    
    private EmailBodyBuilder(EmailBranding brandingStyle, string? caption)
    {
        _caption = caption?.Trim() ?? string.Empty;
        _branding = brandingStyle;
    }
    
    private EmailBodyBuilder(string brandName, string? caption)
        : this(new EmailBranding(brandName), caption)
    {
    }

    public static EmailBodyBuilder Create(EmailBranding brandingStyle, string? caption = null)
        => new(brandingStyle, caption);
    
    public static EmailBodyBuilder Create(string brandName, string? caption = null)
        => new(brandName, caption);
    
    /// <summary>A body paragraph. Blank lines split into separate paragraphs; single newlines become line breaks.
    /// When <paramref name="emphasis"/> is true it renders as a bold, muted label (e.g. "For reference:").</summary>
    public EmailBodyBuilder Paragraph(string text, bool emphasis = false)
    {
        var inner = ToHtmlInner(text);
        var html = emphasis
            ? $"<p style=\"margin: 0 0 6px; color: {_branding.SubtleColour};\"><strong>{inner}</strong></p>"
            : $"<p style=\"margin: 0 0 14px;\">{inner}</p>";
        _sections.Add((Normalise(text), html));
        return this;
    }

    /// <summary>A quoted / fenced block — used for a message or a reporter's original report. Rendered as a
    /// styled &lt;blockquote&gt; in HTML and a dash-fenced block in plain text.</summary>
    public EmailBodyBuilder Quote(string text)
    {
        var html =
            $"<blockquote style=\"margin: 0 0 14px; padding: 10px 14px; border-left: 3px solid {_branding.QuoteBorder}; " +
            $"color: {_branding.SubtleColour}; background: {_branding.QuoteBg};\">{ToHtmlInner(text)}</blockquote>";
        var plain = $"{_branding.PlainRule}\n{Normalise(text)}\n{_branding.PlainRule}";
        _sections.Add((plain, html));
        return this;
    }

    /// <summary>A prominent call-to-action button linking to <paramref name="url"/> (e.g. a confirmation or
    /// reset link). Renders a gradient button in HTML — with a plain-text fallback link beneath, since some
    /// clients strip the button styling — and "<c>text: url</c>" in plain text.</summary>
    public EmailBodyBuilder Button(string text, string url)
    {
        var urlEnc = WebUtility.HtmlEncode(url);
        var textEnc = WebUtility.HtmlEncode(Normalise(text));
        var html =
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin: 8px 0 14px;\"><tr>" +
            $"<td bgcolor=\"{_branding.BrandStart}\" style=\"background: {_branding.BrandStart}; background: linear-gradient(135deg, {_branding.BrandStart}, {_branding.BrandEnd}); border-radius: 6px;\">" +
            $"<a href=\"{urlEnc}\" style=\"display: inline-block; padding: 12px 24px; font-size: 15px; font-weight: bold; color: #ffffff; text-decoration: none;\">{textEnc}</a>" +
            "</td></tr></table>" +
            $"<p style=\"margin: 0 0 14px; color: {_branding.MutedColour}; font-size: 12px;\">Or paste this link into your browser:<br>" +
            $"<a href=\"{urlEnc}\" style=\"color: {_branding.BrandStart}; word-break: break-all;\">{urlEnc}</a></p>";
        _sections.Add(($"{Normalise(text)}:\n{url}", html));
        return this;
    }

    /// <summary>A horizontal rule between sections.</summary>
    public EmailBodyBuilder Divider()
    {
        _sections.Add((_branding.PlainRule, $"<hr style=\"border: none; border-top: 1px solid {_branding.RuleColour}; margin: 20px 0;\">"));
        return this;
    }

    /// <summary>A closing line (e.g. "— The {game} Team"), spaced away from the body above it.</summary>
    public EmailBodyBuilder SignOff(string text)
    {
        _sections.Add((Normalise(text), $"<p style=\"margin: 20px 0 0;\">{ToHtmlInner(text)}</p>"));
        return this;
    }

    /// <summary>A short, muted reference line.</summary>
    public EmailBodyBuilder Reference(string code)
    {
        var encoded = WebUtility.HtmlEncode(code);
        _sections.Add(($"Reference: {code}",
            $"<p style=\"margin: 6px 0 0; color: {_branding.MutedColour}; font-size: 12px;\">Reference: {encoded}</p>"));
        return this;
    }

    /// <summary>A small, muted footer note (e.g. the "you're receiving this because…" disclaimer).</summary>
    public EmailBodyBuilder Footer(string text)
    {
        _sections.Add((Normalise(text),
            $"<p style=\"margin: 14px 0 0; color: {_branding.MutedColour}; font-size: 12px;\">{ToHtmlInner(text)}</p>"));
        return this;
    }

    /// <summary>Renders the accumulated sections into matching plain-text and HTML bodies.</summary>
    public (string Html, string Plain) Build()
    {
        var html = BuildHtml();
        var plain = BuildPlain();
        return (html, plain);
    }

    private string BuildPlain()
    {
        var sb = new StringBuilder();
        sb.Append(_branding.BrandName);
        if (_caption.Length > 0) sb.Append(" — ").Append(_caption);
        sb.Append("\n\n");
        sb.Append(string.Join("\n\n", _sections.Select(s => s.Plain)));
        return sb.ToString();
    }

    private string BuildHtml()
    {
        var caption = _caption.Length > 0
            ? $"<div style=\"font-size: 13px; color: {_branding.HeaderCaption}; margin-top: 2px;\">{WebUtility.HtmlEncode(_caption)}</div>"
            : "";
        var body = string.Join("", _sections.Select(s => s.Html));
        var brandName = WebUtility.HtmlEncode(_branding.BrandName);

        // Table-based, inline-styled shell for maximum email-client compatibility. The gradient header carries a
        // solid bgcolor fallback for clients (e.g. Outlook) that ignore CSS gradients.
        return
            $"<div style=\"background: {_branding.PageBg}; padding: 24px 0; font-family: Arial, Helvetica, sans-serif;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">" +
            "<tr><td align=\"center\">" +
            "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" " +
            $"style=\"width: 600px; max-width: 600px; border-collapse: collapse; background: {_branding.CardBg}; border-radius: 10px; overflow: hidden;\">" +
            "<tr>" +
            $"<td bgcolor=\"{_branding.BrandStart}\" style=\"background: {_branding.BrandStart}; background: linear-gradient(135deg, {_branding.BrandStart}, {_branding.BrandEnd}); padding: 24px 28px;\">" +
            $"<div style=\"font-size: 20px; font-weight: bold; color: #ffffff;\">{brandName}</div>" +
            caption +
            "</td>" +
            "</tr>" +
            "<tr>" +
            $"<td style=\"padding: 24px 28px; font-size: 15px; color: {_branding.TextColour}; line-height: 1.5;\">" +
            body +
            "</td>" +
            "</tr>" +
            "</table>" +
            "</td></tr>" +
            "</table>" +
            "</div>";
    }

    /// <summary>Normalises line endings for plain text (trims trailing whitespace, unifies newlines).</summary>
    private static string Normalise(string text)
        => (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();

    /// <summary>HTML-encodes free text, then maps blank lines to paragraph breaks and single newlines to
    /// &lt;br&gt; — the inner content for a &lt;p&gt; or &lt;blockquote&gt;.</summary>
    private static string ToHtmlInner(string text)
    {
        var encoded = WebUtility.HtmlEncode(Normalise(text));
        var paragraphs = encoded
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim('\n').Replace("\n", "<br>"));
        return string.Join("<br><br>", paragraphs);
    }
}