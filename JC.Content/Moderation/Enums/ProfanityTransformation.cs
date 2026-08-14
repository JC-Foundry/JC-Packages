namespace JC.Content.Moderation.Enums;

/// <summary>
/// What the matcher had to do to the text before a term matched. Drives the confidence score: the
/// more work a match needed, the less certain it is — except where the work itself proves intent.
/// </summary>
[Flags]
public enum ProfanityTransformation
{
    /// <summary>The term matched the text as written.</summary>
    None = 0,

    /// <summary>Only the casing differed. Not evasion, and not penalised.</summary>
    CaseFolded = 1,

    /// <summary>Accents were stripped — 'shít'.</summary>
    DiacriticsRemoved = 2,

    /// <summary>A letter was repeated beyond what the term needs — 'shiiit'.</summary>
    RunExpanded = 4,

    /// <summary>A digit or symbol stood in for a letter — 'sh1t', 'a55'.</summary>
    Leetspeak = 8,

    /// <summary>Characters were inserted between the letters — 's.h.i.t'.</summary>
    SeparatorsRemoved = 16,

    /// <summary>A letter was masked out — 'f*ck'. Evidence of intent rather than of doubt.</summary>
    MaskWildcard = 32,

    /// <summary>The match sits inside a longer word. Caps confidence, so it can never block.</summary>
    InsideWord = 64,

    /// <summary>A letter from another script stood in for a Latin one — Cyrillic 'а' for 'a'.</summary>
    HomoglyphFolded = 128
}
