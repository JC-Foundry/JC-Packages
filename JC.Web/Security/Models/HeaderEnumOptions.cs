namespace JC.Web.Security.Models;

public enum XFrameOptionsMode
{
    Deny,
    SameOrigin
}

public enum ReferrerPolicyMode
{
    NoReferrer,
    NoReferrerWhenDowngrade,
    Origin,
    OriginWhenCrossOrigin,
    SameOrigin,
    StrictOrigin,
    StrictOriginWhenCrossOrigin,
    UnsafeUrl
}

public enum CrossOriginOpenerPolicyMode
{
    UnsafeNone,
    SameOriginAllowPopups,
    SameOrigin,
    NoOpenerAllowPopups
}

public enum CrossOriginResourcePolicyMode
{
    SameSite,
    SameOrigin,
    CrossOrigin
}

public enum CrossOriginEmbedderPolicyMode
{
    UnsafeNone,
    RequireCorp,
    Credentialless
}