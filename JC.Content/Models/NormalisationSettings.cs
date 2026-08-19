namespace JC.Content.Models;

public sealed class NormalisationSettings
{
    public bool Compatibility { get; set; }
    public string LineEnding { get; set; } = "\n";
    
    public bool CollapseWhitespace { get; set; }
    public bool CollapseBlankLines { get; set; }
    public int MaxBlankLines { get; set; } = 1;
    public bool NormaliseQuotes { get; set; }
    public bool NormaliseDashes { get; set; }
    public bool RemoveDiacritics { get; set; }
}