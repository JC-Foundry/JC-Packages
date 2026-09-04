# Release notes

Release notes for JC-Packages **major** versions. Each major release introduces breaking changes and includes a migration guide.
See minor and patch releases below.

| Version             | Date           | Summary                                                                                                                                                                                                     |
|---------------------|----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [v7.0.0](v7.0.0.md) | 25 August 2026 | JC.Identity.Shared updated with new IdentityMiddlewareOptions implementation. Adds the ability to create rule sets so that Identity Middleware fires on specific configured rule sets matching a condition. |
| [v6.0.0](v6.0.0.md) | 11 August 2026 | New JC.Tenancy, JC.Identity.Shared and JC.Identity.Shared.Web packages; tenant filtering becomes opt-in per DbContext; UI framework abstraction and SEO in JC.Web; email attachments                        |
| [v5.0.0](v5.0.0.md) | 17 July 2026   | `IMultiTenancy` and `Tenant` move from JC.Identity to JC.Core, so any package can be tenant-scoped; new JC.FileStorage and JC.FileStorage.Web packages                                                      |
| [v4.0.0](v4.0.0.md) | 4 July 2026    | Multi-DbContext support in the repository layer; context-aware background jobs                                                                                                                              |


**Note**: minor and patch releases were not documented here until version 6 (v6.0.0).

---

---


## Minor releases

### v7.x.x

| Version                  | Date             | Summary                                                                                                                                                                                                                 |
|--------------------------|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [7.1.0](Minor/v7.1.0.md) | 4 September 2026 | New JC.CAP package, single sign-on against CAP as the second identity authority; account-rule redirects carry a return URL; `RoleRecord` replaces the role tuple in `SystemRoles.GetAllRoles`; `LocalUrlHelper` in JC.Core |

### v6.x.x

| Version                  | Date           | Summary                                                                                                                                                                    |
|--------------------------|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [6.1.0](Minor/v6.1.0.md) | 19 August 2026 | New JC.Content package — moderation, diffing, conversion, sanitisation and normalisation; read-only static files in JC.FileStorage; `ContentSanitiser` moves out of JC.Web |


---

---


## Patch releases
**Note**: Patch releases carry no separate release notes document; only the summary listed below.

---

### Version 7
#### v7.1.x

| Version | Packages | Date             | Summary                                                                                                                                                                                                                                                                    |
|---------|----------|------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 7.1.4   | JC.CAP   | 4 September 2026 | `CapLinks.Profile` links to CAP's new profile page, the account's own details; `Manage` is now the account home, listing the applications the account can reach with the tabs to profile, security and personal data. Takes CAP.SSO 1.0.3. |
| 7.1.3   | JC.CAP   | 4 September 2026 | `CapLinks` can carry a return URL: `For(route, returnUrl)` and `RegisterReturningTo` append where CAP should send the user once it is done with them, so an account registered at CAP comes back to the application rather than being left on CAP's account pages. CAP accepts only an absolute URL on an origin the application registered. Takes CAP.SSO 1.0.2. |
| 7.1.2   | JC.CAP   | 4 September 2026 | A role refusal now answers as configured. `CapOptions.AccessDenied` selects a plain 403, the default, CAP's denied page, or a local page at `AccessDeniedPath`. Previously the cookie's framework default redirected to `/Account/AccessDenied`, which no consumer serves. |
| 7.1.1   | JC.CAP   | 4 September 2026 | Update to CAP.SSO from v1.0.0 to v1.0.1.                                                                                                                                                                                                                                   |


#### v7.0.x

| Version | Packages                                                      | Date           | Summary                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|---------|---------------------------------------------------------------|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 7.0.1   | JC.Identity.Shared<br/>JC.Identity.Shared.Web<br/>JC.Identity | 27 August 2026 | `IdentityRuleSet` gains `AdditionalExcludedPaths`, for paths that must stay reachable beyond the set's own access denied, logout and error routes; `ExcludedPaths` is still derived and now carries them too. The claims projection marks the system and unknown identities `IsEnabled`, so neither pseudo-identity reads as a disabled account to anything checking the flag outside the account rules. JC.Identity.Shared.Web and JC.Identity carry no code change and are bumped to keep the identity packages aligned. |

---

### Version 6
#### v6.1.x

| Version | Packages                              | Date           | Summary                                                                                                                                                                                             |
|---------|---------------------------------------|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 6.1.1   | JC.FileStorage<br/>JC.FileStorage.Web | 21 August 2026 | Static files carry a last modified date, read from disk when the file is registered and on every read that reaches it; `AddFileStorageWeb` now forwards the static file parameters it was dropping. |

#### v6.0.x

| Version | Packages | Date           | Summary                                                                                                                                                                                           |
|---------|----------|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 6.0.1   | Global   | 11 August 2026 | Minor bug fixes relating to the new v6 release. Included attribute alignment on AuditEntry to its data mapping, meaning no migration was required; as well a smaller XML and documentation fixes. |

---