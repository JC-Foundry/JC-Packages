namespace JC.Content.Moderation.Enums;

public enum ProfanityTermSource
{
    /// <summary>Curated by JC.Content. Severity and category are assigned deliberately.</summary>
    BuiltIn,

    /// <summary>Derived from the bundled third-party list. Broad coverage, less exact metadata.</summary>
    Imported,

    /// <summary>Registered by the consuming application. Takes precedence over both of the above.</summary>
    Configured
}