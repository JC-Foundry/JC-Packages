namespace JC.Core.Models.Options;

/// <summary>
/// Options controlling audit trail behaviour.
/// </summary>
public class CoreAuditOptions
{
    /// <summary>
    /// The name of this application, stamped onto <c>AuditEntry.SourceApplication</c> so that records
    /// can be attributed to the application that wrote them (useful when several applications share a
    /// database). <c>null</c> by default — leave unset if attribution is not required.
    /// </summary>
    public string? ApplicationName { get; set; }
}
