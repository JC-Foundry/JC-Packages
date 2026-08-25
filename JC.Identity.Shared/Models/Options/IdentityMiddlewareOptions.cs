namespace JC.Identity.Shared.Models.Options;

/// <summary>
/// The account rule sets <see cref="JC.Identity.Shared.Helpers.IdentityRules"/> chooses between:
/// <see cref="RuleSets"/> in order, then <see cref="Default"/>.
/// </summary>
/// <remarks>
/// An application serving more than one audience needs more than one set of these behaviours rather
/// than an exemption from them: a disabled user must still be stopped and a forced reset must still
/// fire, but at that audience's own routes.
/// </remarks>
public class IdentityMiddlewareOptions
{
    /// <summary>Gets or sets the set applied when no entry in <see cref="RuleSets"/> matches.</summary>
    /// <remarks>Always present, so enforcement cannot lapse through an unhandled condition.</remarks>
    public IdentityRuleSet Default { get; set; } = new();

    /// <summary>Gets the conditional sets, tried in order. The first whose condition matches wins.</summary>
    public List<IdentityRuleSet> RuleSets { get; } = [];

    /// <summary>
    /// Adds a set applying to every path beneath <paramref name="pathPrefix"/>, compared
    /// case-insensitively.
    /// </summary>
    /// <param name="pathPrefix">The path prefix the set applies to, and its default name.</param>
    /// <param name="configure">Configures the set's routes and enforcement switches.</param>
    /// <returns>These options, for chaining.</returns>
    public IdentityMiddlewareOptions AddForPathPrefix(string pathPrefix, Action<IdentityRuleSet> configure)
    {
        var ruleSet = new IdentityRuleSet
        {
            Name = pathPrefix,
            Condition = context => context.Path.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase)
        };

        configure(ruleSet);
        RuleSets.Add(ruleSet);

        return this;
    }
}
