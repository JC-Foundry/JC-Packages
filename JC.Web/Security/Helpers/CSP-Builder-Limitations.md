# CSP Builder — Known Limitations & Future Considerations

## Host Source Validation is Pragmatic, Not Spec-Complete

The `HostPattern` regex performs basic sanity checking to reject obvious junk, but it does not implement the full CSP source-expression grammar defined in the specification.

**Known limitations:**
- It approximates "looks like a host or URL" rather than formally parsing CSP `host-source` syntax
- It allows path segments on host sources, which is valid per CSP but can be easy to misunderstand
- The HTTP/HTTPS URL branch requires an actual hostname after the scheme but does not deeply validate URL structure

**Accepted trade-off for v1:** This is sufficient to catch common mistakes (typos, gibberish, malformed values) without over-engineering a full CSP parser. It should not be presented as standards-complete validation.

## Base64 Validation is Character-Level Only

The `Base64Pattern` regex validates that nonce and hash values contain only valid base64/base64url characters and padding. It does not validate:

- Correct padding length relative to the encoded data
- Whether URL-safe and standard base64 characters are mixed
- Structurally valid base64 string length (multiples of 4)

This is intentional — it catches obvious invalid input without being overly strict about encoding details that browsers handle permissively.

## Sandbox Merge Behaviour

Calling `Sandbox()` with no arguments creates the most restrictive sandbox (bare `sandbox` directive). Subsequent calls with tokens (e.g. `Sandbox("allow-scripts")`) add to the existing directive rather than replacing it.

This means:
```csharp
builder.Sandbox().Sandbox("allow-scripts");
// Produces: sandbox allow-scripts
```

The bare `Sandbox()` call is not final — it creates the directive entry which later calls append to. This is by design: start restrictive, then relax as needed.

## Dictionary/Collection Order

Directive output order matches insertion order. This relies on `List<T>` ordering which is guaranteed in .NET, so output is deterministic and stable for testing and debugging.

## `SetDirective` Takes `List<string>`

The internal `SetDirective` method accepts `List<string>` rather than a more abstract type like `IEnumerable<string>`. This is a minor internal API concern — it works fine but could be made more flexible if the builder's internals are ever refactored.

## Future Enhancements

### Custom Directive Escape Hatch

CSP evolves over time. A `CustomDirective(string name, params string[] values)` method would allow consumers to use new or experimental directives without waiting for a builder update. Not required today, but worth considering if the builder is used long-term.

### `child-src` is Legacy Fallback

`child-src` is supported for completeness, but modern CSP usage prefers `frame-src` and `worker-src` directly. `child-src` acts as a fallback for those directives in older browser implementations. Prefer the more specific directives when possible.

### `report-to` Directive vs Reporting API

The `ReportTo()` method sets the CSP `report-to` directive, which references a reporting group name. It does **not** configure the `Reporting-Endpoints` HTTP header or the broader Reporting API pipeline. Consumers must configure those separately for reports to actually be delivered.

## Testing Recommendations

CSP is an area where tests matter more than elegance. The following scenarios should be covered by unit tests:

- **Valid policies** — building common real-world CSP configurations produces correct header strings
- **Invalid directive/keyword combinations** — e.g. `ImgSrc("strict-dynamic")` throws
- **Nonce/hash on wrong directives** — e.g. `ConnectSrc("'nonce-abc123'")` throws
- **Duplicate handling** — calling the same directive twice with the same source does not produce duplicates
- **`'none'` conflicts** — combining `'none'` with other sources throws
- **Report directive replacement** — calling `ReportUri()` or `ReportTo()` twice overwrites the previous value
- **Nonce/hash helper input validation** — passing full tokens (e.g. `'nonce-...'`) to helper methods throws
- **Sandbox token validation** — invalid tokens throw, valid tokens are accepted and deduplicated
