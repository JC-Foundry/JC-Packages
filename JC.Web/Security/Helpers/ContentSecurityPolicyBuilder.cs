using System.Text;
using System.Text.RegularExpressions;

namespace JC.Web.Security.Helpers;

/// <summary>
/// Fluent builder for constructing Content-Security-Policy header values with directive-aware validation.
/// </summary>
public partial class ContentSecurityPolicyBuilder
{
    private readonly List<(string Directive, List<string> Sources)> _directives = [];

    #region Keywords

    private const string KwSelf = "'self'";
    private const string KwUnsafeInline = "'unsafe-inline'";
    private const string KwUnsafeEval = "'unsafe-eval'";
    private const string KwUnsafeHashes = "'unsafe-hashes'";
    private const string KwStrictDynamic = "'strict-dynamic'";
    private const string KwNone = "'none'";
    private const string KwWasmUnsafeEval = "'wasm-unsafe-eval'";
    private const string KwReportSample = "'report-sample'";

    private static readonly Dictionary<string, string> KeywordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["self"] = KwSelf,
        ["'self'"] = KwSelf,
        ["unsafe-inline"] = KwUnsafeInline,
        ["'unsafe-inline'"] = KwUnsafeInline,
        ["unsafe-eval"] = KwUnsafeEval,
        ["'unsafe-eval'"] = KwUnsafeEval,
        ["unsafe-hashes"] = KwUnsafeHashes,
        ["'unsafe-hashes'"] = KwUnsafeHashes,
        ["strict-dynamic"] = KwStrictDynamic,
        ["'strict-dynamic'"] = KwStrictDynamic,
        ["none"] = KwNone,
        ["'none'"] = KwNone,
        ["wasm-unsafe-eval"] = KwWasmUnsafeEval,
        ["'wasm-unsafe-eval'"] = KwWasmUnsafeEval,
        ["report-sample"] = KwReportSample,
        ["'report-sample'"] = KwReportSample,
    };

    // default-src: generic fetch fallback — only basic source expressions
    private static readonly HashSet<string> DefaultSrcKeywords =
    [
        KwSelf, KwNone
    ];

    // script-src: full script keyword set
    private static readonly HashSet<string> ScriptSrcKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwUnsafeEval, KwUnsafeHashes,
        KwStrictDynamic, KwWasmUnsafeEval, KwReportSample
    ];

    // script-src-elem: like script-src but no 'unsafe-hashes' per MDN
    private static readonly HashSet<string> ScriptSrcElemKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwUnsafeEval,
        KwStrictDynamic, KwWasmUnsafeEval, KwReportSample
    ];

    // script-src-attr: inline event handlers — no 'strict-dynamic' or 'wasm-unsafe-eval'
    private static readonly HashSet<string> ScriptSrcAttrKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwUnsafeEval, KwUnsafeHashes, KwReportSample
    ];

    // style-src: full style keyword set
    private static readonly HashSet<string> StyleSrcKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwUnsafeHashes, KwReportSample
    ];

    // style-src-elem: stylesheet elements — no 'unsafe-hashes' (applies to inline attributes only)
    private static readonly HashSet<string> StyleSrcElemKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwReportSample
    ];

    // style-src-attr: inline style attributes
    private static readonly HashSet<string> StyleSrcAttrKeywords =
    [
        KwSelf, KwNone, KwUnsafeInline, KwUnsafeHashes, KwReportSample
    ];

    // General fetch directives: only basic source expressions
    private static readonly HashSet<string> GeneralKeywords =
    [
        KwSelf, KwNone
    ];

    // Directives that accept nonce sources
    private static readonly HashSet<string> NonceCapableDirectives =
    [
        "script-src", "script-src-elem",
        "style-src", "style-src-elem"
    ];

    // Directives that accept hash sources
    private static readonly HashSet<string> HashCapableDirectives =
    [
        "script-src", "script-src-elem", "script-src-attr",
        "style-src", "style-src-elem", "style-src-attr"
    ];

    private static readonly Dictionary<string, HashSet<string>> DirectiveKeywords = new()
    {
        ["default-src"] = DefaultSrcKeywords,
        ["script-src"] = ScriptSrcKeywords,
        ["script-src-elem"] = ScriptSrcElemKeywords,
        ["script-src-attr"] = ScriptSrcAttrKeywords,
        ["style-src"] = StyleSrcKeywords,
        ["style-src-elem"] = StyleSrcElemKeywords,
        ["style-src-attr"] = StyleSrcAttrKeywords,
        ["img-src"] = GeneralKeywords,
        ["font-src"] = GeneralKeywords,
        ["connect-src"] = GeneralKeywords,
        ["media-src"] = GeneralKeywords,
        ["object-src"] = GeneralKeywords,
        ["frame-src"] = GeneralKeywords,
        ["child-src"] = GeneralKeywords,
        ["worker-src"] = GeneralKeywords,
        ["manifest-src"] = GeneralKeywords,
        ["base-uri"] = GeneralKeywords,
        ["form-action"] = GeneralKeywords,
        ["frame-ancestors"] = GeneralKeywords,
    };

    #endregion

    #region Schemes

    private static readonly HashSet<string> Schemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "https:", "http:", "data:", "blob:", "mediastream:", "filesystem:"
    };

    #endregion

    #region Directives

    public ContentSecurityPolicyBuilder DefaultSrc(params string[] sources) => AddDirective("default-src", sources);
    public ContentSecurityPolicyBuilder ScriptSrc(params string[] sources) => AddDirective("script-src", sources);
    public ContentSecurityPolicyBuilder ScriptSrcElem(params string[] sources) => AddDirective("script-src-elem", sources);
    public ContentSecurityPolicyBuilder ScriptSrcAttr(params string[] sources) => AddDirective("script-src-attr", sources);
    public ContentSecurityPolicyBuilder StyleSrc(params string[] sources) => AddDirective("style-src", sources);
    public ContentSecurityPolicyBuilder StyleSrcElem(params string[] sources) => AddDirective("style-src-elem", sources);
    public ContentSecurityPolicyBuilder StyleSrcAttr(params string[] sources) => AddDirective("style-src-attr", sources);
    public ContentSecurityPolicyBuilder ImgSrc(params string[] sources) => AddDirective("img-src", sources);
    public ContentSecurityPolicyBuilder FontSrc(params string[] sources) => AddDirective("font-src", sources);
    public ContentSecurityPolicyBuilder ConnectSrc(params string[] sources) => AddDirective("connect-src", sources);
    public ContentSecurityPolicyBuilder MediaSrc(params string[] sources) => AddDirective("media-src", sources);
    public ContentSecurityPolicyBuilder ObjectSrc(params string[] sources) => AddDirective("object-src", sources);
    public ContentSecurityPolicyBuilder FrameSrc(params string[] sources) => AddDirective("frame-src", sources);
    public ContentSecurityPolicyBuilder ChildSrc(params string[] sources) => AddDirective("child-src", sources);
    public ContentSecurityPolicyBuilder WorkerSrc(params string[] sources) => AddDirective("worker-src", sources);
    public ContentSecurityPolicyBuilder ManifestSrc(params string[] sources) => AddDirective("manifest-src", sources);
    public ContentSecurityPolicyBuilder BaseUri(params string[] sources) => AddDirective("base-uri", sources);
    public ContentSecurityPolicyBuilder FormAction(params string[] sources) => AddDirective("form-action", sources);
    public ContentSecurityPolicyBuilder FrameAncestors(params string[] sources) => AddDirective("frame-ancestors", sources);

    public ContentSecurityPolicyBuilder UpgradeInsecureRequests()
    {
        if (!HasDirective("upgrade-insecure-requests"))
            _directives.Add(("upgrade-insecure-requests", []));

        return this;
    }

    public ContentSecurityPolicyBuilder Sandbox(params string[] values)
    {
        // sandbox with no values = most restrictive
        if (values.Length == 0)
        {
            if (!HasDirective("sandbox"))
                _directives.Add(("sandbox", []));

            return this;
        }

        foreach (var value in values)
        {
            if (!SandboxTokens.Contains(value.Trim().ToLowerInvariant()))
                throw new ArgumentException($"Invalid sandbox token '{value}'.");
        }

        var entry = GetOrCreateDirective("sandbox");

        foreach (var value in values)
        {
            var normalised = value.Trim().ToLowerInvariant();

            if (!entry.Contains(normalised))
                entry.Add(normalised);
        }

        return this;
    }

    public ContentSecurityPolicyBuilder ReportUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Report URI cannot be empty.");

        var trimmed = uri.Trim();

        // Must be a valid relative path (single leading slash, not //) or absolute URI
        if (trimmed.StartsWith('/'))
        {
            if (trimmed.StartsWith("//"))
                throw new ArgumentException($"Invalid report URI '{uri}'. Protocol-relative URIs are not allowed.");
        }
        else if (!Uri.IsWellFormedUriString(trimmed, UriKind.Absolute))
        {
            throw new ArgumentException($"Invalid report URI '{uri}'. Expected a relative path or absolute URI.");
        }

        SetDirective("report-uri", [trimmed]);
        return this;
    }

    public ContentSecurityPolicyBuilder ReportTo(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Report-To group name cannot be empty.");

        SetDirective("report-to", [groupName.Trim()]);
        return this;
    }

    #endregion

    #region Nonce & Hash Helpers

    public ContentSecurityPolicyBuilder ScriptNonce(string nonce) => AddNonceDirective("script-src", nonce);
    public ContentSecurityPolicyBuilder ScriptElemNonce(string nonce) => AddNonceDirective("script-src-elem", nonce);
    public ContentSecurityPolicyBuilder StyleNonce(string nonce) => AddNonceDirective("style-src", nonce);
    public ContentSecurityPolicyBuilder StyleElemNonce(string nonce) => AddNonceDirective("style-src-elem", nonce);

    public ContentSecurityPolicyBuilder ScriptHash(string algorithm, string base64Hash) => AddHashDirective("script-src", algorithm, base64Hash);
    public ContentSecurityPolicyBuilder ScriptElemHash(string algorithm, string base64Hash) => AddHashDirective("script-src-elem", algorithm, base64Hash);
    public ContentSecurityPolicyBuilder StyleHash(string algorithm, string base64Hash) => AddHashDirective("style-src", algorithm, base64Hash);
    public ContentSecurityPolicyBuilder StyleElemHash(string algorithm, string base64Hash) => AddHashDirective("style-src-elem", algorithm, base64Hash);

    private ContentSecurityPolicyBuilder AddNonceDirective(string directive, string nonce)
    {
        if (string.IsNullOrWhiteSpace(nonce))
            throw new ArgumentException("Nonce value cannot be empty.");

        if (nonce.StartsWith("'nonce-", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Pass the raw nonce value only, not the full 'nonce-...' token.");

        if (!Base64Pattern().IsMatch(nonce))
            throw new ArgumentException($"Invalid nonce value '{nonce}'. Expected a base64 string.");

        return AddDirective(directive, [$"'nonce-{nonce}'"]);
    }

    private ContentSecurityPolicyBuilder AddHashDirective(string directive, string algorithm, string base64Hash)
    {
        if (string.IsNullOrWhiteSpace(base64Hash))
            throw new ArgumentException("Hash value cannot be empty.");

        if (base64Hash.StartsWith("'sha", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Pass the raw base64 hash only, not the full 'sha...-...' token.");

        var alg = algorithm.ToLowerInvariant();

        if (alg is not ("sha256" or "sha384" or "sha512"))
            throw new ArgumentException($"Invalid hash algorithm '{algorithm}'. Must be sha256, sha384, or sha512.");

        if (!Base64Pattern().IsMatch(base64Hash))
            throw new ArgumentException($"Invalid base64 hash value '{base64Hash}'.");

        return AddDirective(directive, [$"'{alg}-{base64Hash}'"]);
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds the Content-Security-Policy header value string.
    /// Returns <c>null</c> if no directives have been configured.
    /// </summary>
    public string? Build()
    {
        if (_directives.Count == 0)
            return null;

        var sb = new StringBuilder();

        foreach (var (directive, sources) in _directives)
        {
            if (sb.Length > 0)
                sb.Append("; ");

            sb.Append(directive);

            if (sources.Count > 0)
            {
                sb.Append(' ');
                sb.Append(string.Join(' ', sources));
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Validation

    private static readonly HashSet<string> SandboxTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "allow-downloads", "allow-forms", "allow-modals", "allow-orientation-lock",
        "allow-pointer-lock", "allow-popups", "allow-popups-to-escape-sandbox",
        "allow-presentation", "allow-same-origin", "allow-scripts",
        "allow-top-navigation", "allow-top-navigation-by-user-activation",
        "allow-top-navigation-to-custom-protocols"
    };

    private ContentSecurityPolicyBuilder AddDirective(string directive, string[] sources)
    {
        if (sources.Length == 0)
            throw new ArgumentException($"At least one source is required for '{directive}'.");

        var allowedKeywords = DirectiveKeywords.GetValueOrDefault(directive);
        var validated = new List<string>(sources.Length);

        foreach (var source in sources)
        {
            validated.Add(ValidateSource(directive, source.Trim(), allowedKeywords));
        }

        if (validated.Contains(KwNone) && validated.Count > 1)
            throw new ArgumentException($"'none' cannot be combined with other sources in '{directive}'.");

        var entry = GetOrCreateDirective(directive);

        if (entry.Contains(KwNone) || (validated.Contains(KwNone) && entry.Count > 0))
            throw new ArgumentException($"'none' cannot be combined with other sources in '{directive}'.");

        foreach (var value in validated)
        {
            if (!entry.Contains(value))
                entry.Add(value);
        }

        return this;
    }

    private static string ValidateSource(string directive, string source, HashSet<string>? allowedKeywords)
    {
        // Keyword (with or without quotes) — normalise and validate against directive
        if (KeywordMap.TryGetValue(source, out var normalisedKeyword))
        {
            if (allowedKeywords is null || !allowedKeywords.Contains(normalisedKeyword))
                throw new ArgumentException(
                    $"Keyword {normalisedKeyword} is not valid for directive '{directive}'.");

            return normalisedKeyword;
        }

        var lower = source.ToLowerInvariant();

        // Scheme source
        if (Schemes.Contains(lower))
            return lower;

        // Nonce — validate pattern and directive, return original (base64 is case-sensitive)
        if (NoncePattern().IsMatch(source))
        {
            if (!NonceCapableDirectives.Contains(directive))
                throw new ArgumentException($"Nonce source is not valid for directive '{directive}'.");

            return source;
        }

        // Hash — validate pattern and directive, return original (base64 is case-sensitive)
        if (HashPattern().IsMatch(source))
        {
            if (!HashCapableDirectives.Contains(directive))
                throw new ArgumentException($"Hash source is not valid for directive '{directive}'.");

            return source;
        }

        // Host source — validate against original (preserves casing for URLs)
        if (HostPattern().IsMatch(source))
            return source;

        throw new ArgumentException(
            $"Invalid CSP source '{source}' for directive '{directive}'. " +
            "Expected a keyword (e.g. 'self'), scheme (e.g. https:), nonce, hash, or host/URL.");
    }

    private List<string> GetOrCreateDirective(string directive)
    {
        foreach (var (d, sources) in _directives)
        {
            if (d == directive)
                return sources;
        }

        var newSources = new List<string>();
        _directives.Add((directive, newSources));
        return newSources;
    }

    private void SetDirective(string directive, List<string> sources)
    {
        for (var i = 0; i < _directives.Count; i++)
        {
            if (_directives[i].Directive == directive)
            {
                _directives[i] = (directive, sources);
                return;
            }
        }

        _directives.Add((directive, sources));
    }

    private bool HasDirective(string directive)
    {
        foreach (var (d, _) in _directives)
        {
            if (d == directive)
                return true;
        }

        return false;
    }

    [GeneratedRegex(@"^'nonce-[A-Za-z0-9+/\-_]+=*'$")]
    private static partial Regex NoncePattern();

    [GeneratedRegex(@"^'sha(256|384|512)-[A-Za-z0-9+/\-_]+=*'$")]
    private static partial Regex HashPattern();

    [GeneratedRegex(@"^(\*|(\*\.)?[a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)+(:\d+)?(/\S*)?)$|^https?://[a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)*(:\d+)?(/\S*)?$")]
    private static partial Regex HostPattern();

    [GeneratedRegex(@"^[A-Za-z0-9+/\-_]+=*$")]
    private static partial Regex Base64Pattern();

    #endregion
}
