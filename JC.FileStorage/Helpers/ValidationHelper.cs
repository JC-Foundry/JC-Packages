namespace JC.FileStorage.Helpers;

public static class ValidationHelper
{
    /// <summary>
    /// Hard ceiling on any configured size limit (10GB). No folder or registry default may exceed this.
    /// </summary>
    public const long MaxAllowedBytes = 10L * 1024 * 1024 * 1024;
    
    //Extensions Windows or a shell will execute on open. Blocked outright so the store can never
    //become a malware delivery route - not overridable by a folder or by the registry defaults
    internal static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        //Windows executables, libraries and installers
        ".exe", ".com", ".scr", ".pif", ".dll", ".ocx", ".sys", ".drv", ".cpl",
        ".msi", ".msp", ".mst", ".msix", ".appx", ".application", ".appref-ms", ".gadget",
        //Batch and shell
        ".bat", ".cmd", ".sh", ".bash", ".csh", ".ksh", ".zsh", ".command", ".run",
        //Scripts the Windows shell executes on open
        ".vb", ".vbs", ".vbe", ".js", ".jse", ".ws", ".wsf", ".wsc", ".wsh",
        ".ps1", ".ps1xml", ".ps2", ".ps2xml", ".psc1", ".psc2", ".msc", ".hta",
        //Shell and registry entry points
        ".lnk", ".url", ".scf", ".shb", ".shs", ".inf", ".reg",
        //Other runtimes and platform packages
        ".jar", ".apk", ".app", ".deb", ".rpm", ".dmg", ".pkg"
    };

    /// <summary>
    /// Extensions that can never be stored, whatever a folder or the registry defaults allow.
    /// Compared case-insensitively.
    /// </summary>
    public static IReadOnlyCollection<string> BlockedExtensions => Blocked;

    /// <summary>
    /// Whether <paramref name="extension"/> is blocked outright. The leading dot is optional.
    /// </summary>
    public static bool IsBlockedExtension(string extension)
        => !string.IsNullOrWhiteSpace(extension) && Blocked.Contains(NormalisationHelper.NormaliseExtension(extension));
    
    /// <summary>
    /// Normalises an allowed-extension list and rejects any blocked entry, so a blocked extension
    /// can never be allowed back in by configuration. <c>null</c> means "not set here".
    /// </summary>
    /// <exception cref="ArgumentException">The list is empty, or names a blocked extension.</exception>
    internal static IReadOnlyList<string>? ValidateAllowedExtensions(IEnumerable<string>? allowedExtensions, string paramName)
    {
        if(allowedExtensions == null)
            return null;

        var exts = allowedExtensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => NormalisationHelper.NormaliseExtension(e))
            .Distinct()
            .ToList();

        if(exts.Count == 0)
            throw new ArgumentException("Provide at least one extension, or null for no restriction.", paramName);

        var blocked = exts.Where(Blocked.Contains).ToList();
        if(blocked.Count > 0)
            throw new ArgumentException(
                $"These extensions are blocked and cannot be allowed: {string.Join(", ", blocked)}.", paramName);

        return exts;
    }
    
    /// <summary>
    /// Checks a size limit against the <see cref="MaxAllowedBytes"/> ceiling, returning it unchanged
    /// when valid. <c>null</c> means "no limit set here" and is always valid.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The limit is zero or negative, or above the ceiling.</exception>
    internal static long? ValidateMaxBytes(long? maxBytes, string paramName)
    {
        switch (maxBytes)
        {
            case null:
                return null;
            case <= 0:
                throw new ArgumentOutOfRangeException(paramName, maxBytes, "A size limit must be greater than zero.");
            case > MaxAllowedBytes:
                throw new ArgumentOutOfRangeException(paramName, maxBytes,
                    $"A size limit cannot exceed {MaxAllowedBytes} bytes (10GB).");
            default:
                return maxBytes;
        }
    }
}