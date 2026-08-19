using JC.Content.Comparison.Services;
using JC.Content.Conversion.Enums;
using JC.Content.Conversion.Services;
using JC.Content.Helpers;
using JC.Content.Models;
using JC.Content.Moderation.Models;
using JC.Content.Moderation.Services;

namespace JC.Content.Services;

public class ContentManager
{
    private readonly ProfanityMasker _profanityMasker;
    private readonly ContentConverter _contentConverter;
    private readonly ContentComparer _contentComparer;

    public ContentManager(ProfanityMasker profanityMasker,
        ContentConverter contentConverter,
        ContentComparer contentComparer)
    {
        _profanityMasker = profanityMasker;
        _contentConverter = contentConverter;
        _contentComparer = contentComparer;
    }

    /// <summary>
    /// Normalises and moderates the given content based on the provided settings.
    /// This method processes the content to ensure formatting consistency and applies profanity masking according to the specified configurations.
    /// </summary>
    /// <param name="content">The text content to be processed. Can be null, in which case no changes will be applied.</param>
    /// <param name="settings">Optional settings to define how the content should be normalised and moderated. Default settings will be applied if null.</param>
    /// <returns>A ManagerResponse object containing the result of the moderation process,
    /// including information on profanities and their treatment in the content.</returns>
    public ManagerResponse NormaliseAndModerate(string? content, ManagerSettings? settings = null)
    {
        settings ??= new ManagerSettings();
        var normalised = Normalise(content, settings.NormalisationSettings);
        var moderationResult = Moderate(normalised, settings.ProfanitySettings);
        
        return new ManagerResponse(content, moderationResult);
    }

    /// <summary>
    /// Normalises, moderates and converts the given content based on the specified settings.
    /// Moderation runs on whichever side is not HTML: before conversion when the target is HTML, after it otherwise.
    /// </summary>
    /// <remarks>Mask and tag text is inserted verbatim, so a replacement that is syntax in the format being moderated is read as syntax.</remarks>
    /// <param name="content">The text content to be processed. Can be null, in which case no changes will be applied.</param>
    /// <param name="settings">Optional settings to define how the content should be processed during normalisation, moderation, and conversion. If null, default settings will be used.</param>
    /// <returns>A ManagerConvertResponse object containing the finished content and the moderation result. The moderation result, and every match index in it, describes the source content when the target is HTML and the converted output otherwise.</returns>
    public ManagerConvertResponse NormaliseModerateAndConvert(string? content, ManagerConvertSettings? settings = null)
    {
        settings ??= new ManagerConvertSettings();
        var normalised = Normalise(content, settings.NormalisationSettings);

        ProfanityModerationMaskResult moderationResult;
        string? updatedContent;
        if (settings.TargetFormat == ContentFormat.Html)
        {
            //Target is HTML, so moderate before convert
            moderationResult = Moderate(normalised, settings.ProfanitySettings);
            updatedContent = _contentConverter.Convert(moderationResult.UpdatedContent, settings.SourceFormat, settings.TargetFormat);
        }
        else
        {
            //Target is not HTML, so the converted output is the safe side to moderate
            var convertedContent = _contentConverter.Convert(normalised, settings.SourceFormat, settings.TargetFormat);
            moderationResult = Moderate(convertedContent, settings.ProfanitySettings);
            updatedContent = moderationResult.UpdatedContent;
        }
        
        return new ManagerConvertResponse(content, moderationResult, updatedContent);
    }

    /// <summary>
    /// Normalises, moderates, and compares two pieces of content based on the provided settings.
    /// This method ensures formatting consistency, applies profanity masking, and evaluates differences between the two content inputs.
    /// </summary>
    /// <param name="content">The first text content to be processed. Can be null, in which case no changes will be applied.</param>
    /// <param name="compareContent">The content to compare against the first content. Can be null, in which case no changes will be applied.</param>
    /// <param name="settings">Optional settings to define how normalisation, moderation, and comparison should be performed. Default settings will be applied if null.</param>
    /// <returns>A ManagerCompareResponse object containing the results of the normalisation, moderation, and comparison processes,
    /// including information on modifications made to each content and details of the comparison result.</returns>
    public ManagerCompareResponse NormaliseModerateAndCompare(string? content, string? compareContent, ManagerCompareSettings? settings = null)
    { 
        settings ??= new ManagerCompareSettings(); 
        var normalised = Normalise(content, settings.NormalisationSettings); 
        var normalisedCompareContent = Normalise(compareContent, settings.NormalisationSettings);
        
        var moderationResult = Moderate(normalised, settings.ProfanitySettings); 
        var moderationCompareResult = Moderate(normalisedCompareContent, settings.ProfanitySettings);
        
        var comparisonResult = _contentComparer.Compare(
           moderationResult.UpdatedContent, moderationCompareResult.UpdatedContent, settings.GranularityOverride);
        
        return new ManagerCompareResponse(content, compareContent, moderationResult, moderationCompareResult, comparisonResult);
    }


    #region Private Internals

    private string? Normalise(string? content, NormalisationSettings settings)
    {
        var normalisedContent = NormalisationHelper.Normalise(content, settings.Compatibility, settings.LineEnding);
        if(settings.CollapseWhitespace)
            normalisedContent = NormalisationHelper.CollapseWhitespace(normalisedContent);
        
        if(settings.CollapseBlankLines)
            normalisedContent = NormalisationHelper.CollapseBlankLines(normalisedContent, settings.MaxBlankLines, settings.LineEnding);
        
        if(settings.NormaliseQuotes)
            normalisedContent = NormalisationHelper.NormaliseQuotes(normalisedContent);
        
        if(settings.NormaliseDashes)
            normalisedContent = NormalisationHelper.NormaliseDashes(normalisedContent);
        
        if(settings.RemoveDiacritics)
            normalisedContent = NormalisationHelper.RemoveDiacritics(normalisedContent);
        
        return normalisedContent;
    }

    private ProfanityModerationMaskResult Moderate(string? content, ProfanitySettings settings)
    {
        ProfanityModerationMaskResult result;
        switch (settings.MaskType)
        {
            case ProfanitySettings.ProfanityMaskType.Remove:
                result = _profanityMasker.AnalyseAndRemove(content, settings.LevelOverride);
                break;
            case ProfanitySettings.ProfanityMaskType.Tag:
                result = _profanityMasker.AnalyseAndTag(content, settings.TagFormat ?? ProfanityMasker.GenericTag, settings.LevelOverride);
                break;
            case ProfanitySettings.ProfanityMaskType.Mask:
            default:
                result = _profanityMasker.AnalyseAndMask(content, settings.MaskChar ?? '*', 
                    settings.CappedMaskLength == null, settings.CappedMaskLength, settings.LevelOverride);
                break;
        }
        
        return result;
    }

    #endregion
}