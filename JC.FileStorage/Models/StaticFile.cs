using JC.FileStorage.Helpers;

namespace JC.FileStorage.Models;

public class StaticFile
{
    public string Key => Path.Combine(SubFolders.Aggregate(string.Empty, Path.Combine), FileName).ToLowerInvariant();
    
    public string Name { get; }
    public string Extension { get; }
    public string FileName => $"{Name}{Extension}";
    
    public IReadOnlyList<string> SubFolders { get; } = [];

    private StaticFile(string name, string extension)
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
    }

    public StaticFile(string fileName)
        : this(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName))
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
}