# Specification pin

The implemented `template.json` contract and the sources it was verified against. Update this
record when adopting upstream changes — corpus first, then implementation.

## Implemented contract version

- Engine line: `10.0.401` (SDK 10)
- All 24 top-level properties of the published schema, plus `globalCustomOperations` and
  `specialCustomOperations` which the live engine reads and the published schema omits.
- All 11 published generators plus `evaluate`.

## Sources, in precedence order

On conflict, the higher source wins.

| Precedence | Source | Status |
| --- | --- | --- |
| 1 | `dotnet/dotnet` → `src/sdk/src/TemplateEngine/**` | live engine source |
| 2 | `json.schemastore.org/template` (draft-07) | the published schema enumeration |
| 3 | `dotnet/templating` → `docs/**` on `main` | the prose reference for behavior |

The upstream repository's default branch is `archive` — its `main` branch still carries current
documentation, but engine code must be read from `dotnet/dotnet`.

## Documented behavior verified during implementation

- `sourceName` default value forms: `identity`, `safe_namespace` (namespace), `safe_name`
  (class name), `lower_safe_namespace`, `lower_safe_name` — with the segment rules from
  `docs/Naming-and-default-value-forms.md` (e.g. `Template.1` → `Template__1` /
  `Template._1`).
- User-defined value form identifiers from the schema enumeration: `replace`, `chain`,
  `xmlEncode`, `jsonEncode`, `lowerCase`, `lowerCaseInvariant`, `upperCase`,
  `upperCaseInvariant`, `firstLowerCase`, `firstLowerCaseInvariant`, `firstUpperCase`,
  `firstUpperCaseInvariant`, `titleCase`, `kebabCase`, `snakeCase`, `identity`, `safe_name`,
  `lower_safe_name`, `safe_namespace`, `lower_safe_namespace`.
- Condition evaluators: `C++`, `C++2` (default for computed symbols and switch cases),
  `MSBUILD`, `VB`; multi-choice `==` behaves as contains; quoteless literals are the
  `enableQuotelessLiterals` opt-in.
- Conditional file families and directive tokens per
  `docs/Conditional-processing-and-comment-syntax.md`, including the actionable variants,
  `cnd:noEmit` on/off markers, and MSBuild `Condition` attribute processing.
- `guids` entries replace GUIDs found in the source with generated GUIDs, preserving the format
  and casing of the occurrence.

## Deliberate deviations

These diverge from `dotnet new` by design and are documented in the CLI documentation:

- `cratis new` exit codes follow this repository's contract (`0` created, `1` creation or a
  required post action failed, `2` could not run) rather than `dotnet new`'s numeric codes.
- The restore post action runs `dotnet restore` only when dotnet is on PATH; otherwise it is
  reported as not performed with manual instructions — the scaffold itself still succeeds.
- Template parameter help is exposed as `cratis new <template> --parameters` because the CLI
  framework intercepts `--help` before per-template parameters can be rendered.
- The `flag`, `include`, `region`, `balancedNesting` and `expandVariables` custom operations are
  implemented with the configuration surface documented above; configurations outside that
  surface fail loudly with a named error rather than being ignored.
- The legacy `onlyIf` property on parameter and generated symbols is accepted at parse time (the
  upstream corpus carries it in valid templates), but its anchored-replacement semantics —
  replacing the token only between the `after`/`before` anchors — are not yet implemented:
  replacements currently apply to the whole file. The corpus groups
  `TemplateWithOnlyIfForLocalhost` and `TemplateWithOnlyIfStatement` exercise it and are the
  templates to assert against once it lands.

## Conformance

- The upstream `Microsoft.TemplateEngine.TestTemplates` corpus (MIT) is vendored under
  `Source/Templating.Conformance/UpstreamCorpus/` at upstream commit
  `9b003d9b46874d535955f03da278d4d5a9643230` (2026-07-02, `main`) — 56 groups, 100 manifests,
  provenance recorded next to it. The phase-one gate holds: 98 manifests parse, the remaining 2
  are the intentionally-invalid `Invalid/MissingIdentity` and `Invalid/MissingMandatoryConfig`
  templates failing with named errors, and zero manifests fail with any other exception.
  Curated in-tree templates cover conditional families, value forms, generators, renames,
  `copyOnly`, exclusions, and the loud-failure contract for unsupported constructs.
- Differential testing against `dotnet new` (render both ways, diff, normalize GUIDs,
  timestamps and resolved versions) runs as a test-time oracle only, on machines with an SDK.
