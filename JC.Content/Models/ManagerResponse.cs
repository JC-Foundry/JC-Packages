using JC.Content.Comparison.Models;
using JC.Content.Moderation.Models;

namespace JC.Content.Models;

public class ManagerResponse(string? originalContent, ProfanityModerationMaskResult profanityModerationMaskResult)
{
    public string? OriginalContent { get; } = originalContent;
    public ProfanityModerationMaskResult ProfanityModerationMaskResult { get; } = profanityModerationMaskResult;
}

public sealed class ManagerConvertResponse(string? originalContent, ProfanityModerationMaskResult profanityModerationMaskResult,
    string? convertedContent) 
    : ManagerResponse(originalContent, profanityModerationMaskResult)
{
    public string? ConvertedContent { get; } = convertedContent;
}

public sealed class ManagerCompareResponse(
    string? originalContent,
    string? originalComparedContent,
    ProfanityModerationMaskResult profanityModerationMaskResult,
    ProfanityModerationMaskResult comparedProfanityModerationMaskResult,
    ContentComparisonResult contentComparisonResult)
    : ManagerResponse(originalContent, profanityModerationMaskResult)
{
    public string? OriginalComparedContent { get; } = originalComparedContent;

    public ContentComparisonResult ContentComparisonResult { get; } = contentComparisonResult;

    public ProfanityModerationMaskResult ComparedProfanityModerationMaskResult { get; } = comparedProfanityModerationMaskResult;
}