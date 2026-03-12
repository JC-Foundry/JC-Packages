using JC.Core.Enums;

namespace JC.Communication.Messaging.Models;

public class ChatThreadParams : QueryParams
{
    public string? Name { get; internal set; }
    public string? Description { get; }
    public string DateFormat { get; } = "g";
    public bool PreferHexCode { get; } = true;
    
    public ChatThreadParams(string name, bool asNoTracking = false, 
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        : base(asNoTracking, deletedQueryType)
    {
        Name = name;
    }

    public ChatThreadParams(string name, string? description = null, string? dateFormat = null,
        bool preferHexCode = true, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        : base(asNoTracking, deletedQueryType)
    {
        Name = name;
        Description = description;
        DateFormat = dateFormat ?? "g";
        PreferHexCode = preferHexCode;
    }
}

public class QueryParams
{
    public bool AsNoTracking { get; } = true;
    public DeletedQueryType DeletedQueryType { get; } = DeletedQueryType.OnlyActive;

    public QueryParams()
    {
    }

    public QueryParams(bool asNoTracking, 
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        AsNoTracking = asNoTracking;
        DeletedQueryType = deletedQueryType;
    }
}