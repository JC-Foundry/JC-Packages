using JC.Content.Conversion.Models.Options;
using Markdig;

namespace JC.Content.Conversion.Services;

/// <summary>Converts Markdown into HTML, which is the route to every other format.</summary>
internal sealed class MarkdownConverter
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownConverter(ContentConversionOptions options)
    {
        var builder = new MarkdownPipelineBuilder();

        if(options.GithubFlavoured)
            builder.UsePipeTables().UseEmphasisExtras().UseTaskLists().UseAutoLinks();

        //Markdown permits raw HTML, so a document from an untrusted author can carry markup through
        //untouched. Disabling it here removes that route rather than relying on a later sanitise
        if(!options.AllowRawHtml)
            builder.DisableHtml();

        _pipeline = builder.Build();
    }

    public string ToHtml(string markdown) => Markdown.ToHtml(markdown, _pipeline);
}
