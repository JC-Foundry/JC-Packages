namespace JC.BackgroundJobs.Services;

/// <summary>
/// Produces stable, human-readable names for job types — including closed generic
/// jobs such as <c>AuditCleanupJob&lt;PortfolioDbContext&gt;</c>.
/// <see cref="Type.Name"/> collapses every closed generic of the same open type to
/// the same arity-mangled name (e.g. <c>AuditCleanupJob`1</c>), which would cause
/// distinct per-context jobs to share a Hangfire job ID and silently overwrite one another.
/// </summary>
internal static class JobNaming
{
    /// <summary>
    /// Builds a display/identity name for the job type. Non-generic types return their
    /// plain name; closed generic types include their type arguments, e.g.
    /// <c>AuditCleanupJob(PortfolioDbContext)</c>. Nested generics are expanded recursively.
    /// </summary>
    public static string GetName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];

        var args = type.GetGenericArguments().Select(GetName);
        return $"{name}({string.Join(", ", args)})";
    }
}
