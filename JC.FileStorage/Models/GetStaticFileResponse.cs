namespace JC.FileStorage.Models;

public abstract record GetStaticFileResponseBase : ResponseBase
{
    public StaticFile? File { get; init; }
    
    protected GetStaticFileResponseBase(StaticFile file)
    {
        File = file;
    }
    
    protected GetStaticFileResponseBase(string errorMessage)
        : base(errorMessage)
    {
    }
}

public record GetStaticFileByteResponse : GetStaticFileResponseBase
{
    public byte[]? FileContent { get; init; }
    
    public GetStaticFileByteResponse(StaticFile file, byte[] fileContent) 
        : base(file)
    {
        FileContent = fileContent;
    }
    
    public GetStaticFileByteResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}

public record GetStaticFileTextResponse : GetStaticFileResponseBase
{
    public string? FileContentText {get; init; }

    public GetStaticFileTextResponse(StaticFile file, string fileContentText) 
        : base(file)
    {
        FileContentText = fileContentText;
    }

    public GetStaticFileTextResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}