using JC.Content.Helpers;
using JC.Content.Moderation.Services;

namespace JC.Content.Services;

//TODO: This will build out as we add more features:
//its intended to be a pipeline manager for dealing with content:
// - such as: raw -> normalise -> moderate -> return
//            raw -> normalise -> moderate -> compare -> return
//            raw -> normalise -> moderate -> convert -> return

public class ContentManager
{
    private readonly ProfanityMasker _profanityMasker;

    public ContentManager(ProfanityMasker profanityMasker)
    {
        _profanityMasker = profanityMasker;
    }

    public string? ProcessContent(string? content)
        => _profanityMasker.AnalyseAndMask(NormalisationHelper.Normalise(content)).UpdatedContent;
}