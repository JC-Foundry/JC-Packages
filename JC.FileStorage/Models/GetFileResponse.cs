namespace JC.FileStorage.Models;

public abstract record ResponseBase
{
    public bool Result { get; }
    public string? ErrorMessage { get; }

    internal ResponseBase()
    {
        Result = true;
    }

    internal ResponseBase(string errorMessage)
    {
        Result = false;
        ErrorMessage = errorMessage;
    }
}

public abstract record GetFileResponseBase : ResponseBase
{
    public SavedFile? File { get; init; }

    protected GetFileResponseBase(SavedFile file)
    {
        File = file;
    }

    protected GetFileResponseBase(string errorMessage)
        : base(errorMessage)
    {
    }
}

public record GetFileByteResponse : GetFileResponseBase
{
    public byte[]? FileContent { get; init; }

    public GetFileByteResponse(SavedFile file, byte[] fileContent) 
        : base(file)
    {
        FileContent = fileContent;
    }

    public GetFileByteResponse(string errorMessage) 
        : base(errorMessage)
    {
    }
}

public record GetFileTextResponse : GetFileResponseBase
{
    public string? FileContentText { get; init; }
    
    public GetFileTextResponse(SavedFile file, string fileContentText) 
        : base(file)
    {
        FileContentText = fileContentText;
    }
    
    public GetFileTextResponse(string errorMessage) 
        : base(errorMessage)
    {
    }
}