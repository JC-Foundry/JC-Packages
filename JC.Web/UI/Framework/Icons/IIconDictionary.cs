namespace JC.Web.UI.Framework.Icons;

/// <summary>
/// Marker for a package's icon class dictionary. Each package defines its own contract deriving
/// from this, exactly as it does for <see cref="IFrameworkDictionary"/>.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="IFrameworkDictionary"/> because the two are selected by different
/// choices — a class dictionary by <see cref="UIFrameworkService.Framework"/>, an icon dictionary by
/// <see cref="UIFrameworkService.IconFramework"/>. An icon set is a different library from a CSS
/// framework and the two are picked independently, so a Tailwind application may well still use
/// Bootstrap Icons. A package may need one dictionary, the other, or both; JC.Web itself declares no
/// icon contract because none of its components render a glyph.
/// <para>
/// As with class dictionaries, every value is a <b>complete</b> class attribute value —
/// <c>"bi bi-bell"</c>, not <c>"bi-bell"</c>. Font Awesome's equivalent is <c>"fa-solid fa-bell"</c>,
/// which shares no base class with it, so storing the finished value is the only thing that works
/// for both.
/// </para>
/// </remarks>
public interface IIconDictionary;
