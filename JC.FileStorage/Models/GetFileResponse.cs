namespace JC.FileStorage.Models;

public abstract record GetFileResponseBase
{
    public bool Result { get; init; }
    public SavedFile? File { get; init; }
    public string? ErrorMessage { get; init; }

    public GetFileResponseBase(SavedFile file)
    {
        Result = true;
        File = file;
    }

    public GetFileResponseBase(string errorMessage)
    {
        Result = false;
        ErrorMessage = errorMessage;
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