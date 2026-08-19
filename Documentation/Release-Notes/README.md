# Release notes

Release notes for JC-Packages **major** versions. Each major release introduces breaking changes and includes a migration guide.
See minor and patch releases below.

| Version             | Date           | Summary                                                                                                                                                                              |
|---------------------|----------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [v6.0.0](v6.0.0.md) | 11 August 2026 | New JC.Tenancy, JC.Identity.Shared and JC.Identity.Shared.Web packages; tenant filtering becomes opt-in per DbContext; UI framework abstraction and SEO in JC.Web; email attachments |
| [v5.0.0](v5.0.0.md) | 17 July 2026   | `IMultiTenancy` and `Tenant` move from JC.Identity to JC.Core, so any package can be tenant-scoped; new JC.FileStorage and JC.FileStorage.Web packages                               |
| [v4.0.0](v4.0.0.md) | 4 July 2026    | Multi-DbContext support in the repository layer; context-aware background jobs                                                                                                       |


**Note**: minor and patch releases were not documented here until version 6 (v6.0.0).

---

---


## Minor releases

### v6.x.x

| Version                          | Date           | Summary                                                                                                                                                                     |
|----------------------------------|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [6.1.0](Minor/v6.1.0.md)         | 19 August 2026 | New JC.Content package — moderation, diffing, conversion, sanitisation and normalisation; read-only static files in JC.FileStorage; `ContentSanitiser` moves out of JC.Web |


---

---


## Patch releases
**Note**: Patch releases carry no separate release notes document; only the summary listed below.

---

### Version 6
#### v6.1.x

| Version | Date | Summary |
|---------|------|---------|
| -       | -    | -       |

#### v6.0.x

| Version | Date           | Summary                                                                                                                                                                                           |
|---------|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 6.0.1   | 11 August 2026 | Minor bug fixes relating to the new v6 release. Included attribute alignment on AuditEntry to its data mapping, meaning no migration was required; as well a smaller XML and documentation fixes. |

---