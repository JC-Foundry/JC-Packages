using JC.Content.Comparison.Enums;
using JC.Content.Conversion.Enums;

namespace JC.Content.Models;

public class ManagerSettings
{
    public ProfanitySettings ProfanitySettings { get; set; } = new();
    public NormalisationSettings NormalisationSettings { get; set; } = new();
}

public sealed class ManagerConvertSettings : ManagerSettings
{
    public ContentFormat SourceFormat { get; private set; } = ContentFormat.Markdown;
    public ContentFormat TargetFormat { get; private set; } = ContentFormat.Html;

    public bool ChangeFormats(ContentFormat? sourceFormat = null, ContentFormat? targetFormat = null)
    {
        var changeSource = sourceFormat != null;
        var changeTarget = targetFormat != null;
        if(!changeSource && !changeTarget)
            return false;
        
        //Validate
        switch (changeSource)
        {
            case true when changeTarget && sourceFormat == targetFormat:
                //Changing both, and both match
                return false;
            case true when !changeTarget:
            {
                //Changing source, but not target, so check new source is different from current target
                if(sourceFormat == TargetFormat)
                    return false;
                break;
            }
            case false when changeTarget:
            {
                //Changing target, but not source, so check new target is different from current source
                if(SourceFormat == targetFormat)
                    return false;
                break;
            }
        }
        
        //Passed all checks, so update
        SourceFormat = changeSource ? sourceFormat!.Value : SourceFormat;
        TargetFormat = changeTarget ? targetFormat!.Value : TargetFormat;
        return true;
    }
}

public sealed class ManagerCompareSettings : ManagerSettings
{
    public ComparisonGranularity? GranularityOverride { get; set; }
}