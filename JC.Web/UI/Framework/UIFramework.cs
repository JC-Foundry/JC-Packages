namespace JC.Web.UI.Framework;

[Flags]
public enum UIFramework
{
    Bootstrap = 1,
    Tailwind = 2,
    CustomJCTailwind = 4
}

public class UIFrameworkService
{
    /// <summary>
    /// Creates the service, resolving the supplied flags to a single framework.
    /// </summary>
    /// <param name="framework">The configured framework, which may combine flags.</param>
    public UIFrameworkService(UIFramework framework)
    {
        // Resolved once, here, so nothing downstream ever handles unresolved flags — anything
        // reading Framework can switch on it directly.
        Framework = GetUIFramework(framework);
    }

    /// <summary>
    /// The single framework to render for. Always one value, never a combination.
    /// </summary>
    public UIFramework Framework { get; }


    /// <summary>
    /// Retrieves the specified UI framework based on the provided framework flags.
    /// </summary>
    /// <param name="frameworkFlags">A combination of one or more UIFramework flags to evaluate.</param>
    /// <returns>The UIFramework that matches the provided flags, defaulting to Bootstrap if no specific match is found.</returns>
    private static UIFramework GetUIFramework(UIFramework frameworkFlags)
    {
        if (frameworkFlags.HasFlag(UIFramework.CustomJCTailwind))
            return UIFramework.CustomJCTailwind;

        return frameworkFlags.HasFlag(UIFramework.Tailwind) 
            ? UIFramework.Tailwind 
            : UIFramework.Bootstrap;
    }
}