using JC.FileStorage.Helpers;
using JC.FileStorage.Services;

namespace JC.FileStorage.Models;

public class StaticFile
{
    public string Key => Path.Combine(SubFolders.Aggregate(string.Empty, Path.Combine), FileName).ToLowerInvariant();
    
    public string Name { get; }
    public string Extension { get; }
    public string FileName => $"{Name}{Extension}";
    
    public DateTime? LastModifiedUtc { get; internal set; }
    public string? LastModified(string format) => LastModifiedUtc?.ToLocalTime().ToString(format);
    
    public IReadOnlyList<string> SubFolders { get; } = [];

    private StaticFile(string name, string extension, DateTime? lastModifiedUtc = null)
    {
        if(name == null)
            throw new ArgumentException("Name cannot be null", nameof(name));
        
        if(string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be null or whitespace", nameof(extension));
        
        var ext = NormalisationHelper.NormaliseExtension(extension, false);
        var fileName = $"{name}{ext}";
        name = Path.GetFileNameWithoutExtension(fileName);
        ext = Path.GetExtension(fileName);
        
        Name = name;
        Extension = ext;
        LastModifiedUtc = lastModifiedUtc;
    }

    public StaticFile(string fileName, DateTime? lastModifiedUtc = null)
        : this(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName), lastModifiedUtc)
    {
    }
    
    private StaticFile(string name, string extension, params IEnumerable<string> subFolders)
        : this(name, extension)
    {
        var subFolderList = subFolders.ToList();
        SubFolders = subFolderList.AsReadOnly();
    }

    public StaticFile(string fileName, params IEnumerable<string> subFolders)
        : this(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName), subFolders)
    {
    }

    /// <summary>
    /// Updates the <see cref="LastModifiedUtc"/> property with the most recent modification timestamp
    /// of the file represented by this instance, using the provided file path provider.
    /// </summary>
    /// <param name="pathProvider">
    /// An instance of <see cref="FilePathProvider"/> used to retrieve the last modified timestamp
    /// of the file from the filesystem or storage.
    /// </param>
    public void RefreshLastModified(FilePathProvider pathProvider)
    {
        LastModifiedUtc = pathProvider.GetLastModifiedUtc(FileName, SubFolders);
    }
}