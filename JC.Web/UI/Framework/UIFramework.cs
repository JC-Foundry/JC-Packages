namespace JC.Web.UI.Framework;

[Flags]
public enum UIFramework
{
    Bootstrap = 1,
    Tailwind = 2,
    CustomJCTailwind = 4
}

[Flags]
public enum IconFramework
{
    Bootstrap = 1,
    FontAwesome = 2
}

public class UIFrameworkService
{
    /// <summary>
    /// Creates the service, resolving the supplied flags to a single framework.
    /// </summary>
    /// <param name="framework">The configured framework, which may combine flags.</param>
    /// <param name="iconFramework">The configured icon framework, which may combine flags.</param>
    public UIFrameworkService(UIFramework framework, IconFramework iconFramework)
    {
        // Resolved once, here, so nothing downstream ever handles unresolved flags — anything
        // reading Framework can switch on it directly.
        if (framework.HasFlag(UIFramework.CustomJCTailwind))
            //Always prefer CustomJCTailwind over Tailwind/Bootstrap
            Framework = UIFramework.CustomJCTailwind;
        else 
            Framework = framework.HasFlag(UIFramework.Tailwind) 
                //Always prefer Tailwind over Bootstrap
                ? UIFramework.Tailwind 
                : UIFramework.Bootstrap;
        
        IconFramework = iconFramework.HasFlag(IconFramework.Bootstrap) || iconFramework == 0
            //Always prefer Bootstrap over FontAwesome
            ? IconFramework.Bootstrap
            : IconFramework.FontAwesome;
    }

    /// <summary>
    /// The single framework to render for. Always one value, never a combination.
    /// </summary>
    public UIFramework Framework { get; }
    
    public IconFramework IconFramework { get; }
}