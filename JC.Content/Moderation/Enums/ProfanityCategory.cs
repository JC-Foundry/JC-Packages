namespace JC.Content.Moderation.Enums;

public enum ProfanityCategory
{
    /// <summary>No category assigned.</summary>
    None,

    /// <summary>General profanity and swearing.</summary>
    General,

    /// <summary>Sexual acts, anatomy and pornography.</summary>
    Sexual,

    /// <summary>Slurs and abuse directed at race, ethnicity or nationality.</summary>
    Racial,

    /// <summary>Slurs and abuse directed at sexuality or gender identity.</summary>
    Sexuality,

    /// <summary>Religious profanity, blasphemy and religious abuse.</summary>
    Religious,

    /// <summary>Extreme or shock content, including references to violence and abuse.</summary>
    Shock,

    /// <summary>A category defined by the consuming application.</summary>
    Custom
}