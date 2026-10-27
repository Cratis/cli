---
description: Generate, conform and validate Cratis Screenplay documents from Arc or Marten application source with the cratis screenplay commands.
---

# Screenplay

`cratis screenplay` works with Cratis Screenplay (`.play`) documents. It generates one from Arc, Marten, or Marten + Wolverine application source — so the event model your team reads is derived from the code that actually runs rather than maintained alongside it — and it compiles the documents you already have.

```bash
cratis render [PATH] --name MyApplication
cratis render --workspace application.workspace.json
cratis screenplay generate [PATH]
cratis screenplay conform <MODEL_ROOT> [--project <PATH>]
cratis screenplay conform-namespace <MODEL_ROOT> --event-store <NAME> --namespace <NAME> [--sequence <ID>]
cratis screenplay validate [PATH]
cratis screenplay baseline check [ROOT]
cratis screenplay baseline admit [ROOT] --release <LABEL> [--from-esm <FILE>]
cratis screenplay mcp [PATH]
cratis screenplay desktop install
```

The CLI bundles Screenplay 4.250.0 (including its MCP server), Stage 4.201.1, and the Arc source adapter 22.54.0. `cratis render` admits ESM v1 through v32, including the canonical screen-composition corpus observable query and query-only specification shapes, and preserves authored Scene layouts, profiles, templates, navigation and forms when a model declares screens. Guarded screen actions render as runnable Scene guards: the generated frontend evaluates the authored guard, including enum-typed comparisons against the read model's declared members, and shows the action only when it passes. The renderer still refuses unsupported constructs within those versions: non-event triggers and unsupported captures or Translate slices report typed Stage diagnostics, while supported inbound and outbound Translate slices render as Chronicle reactors, including ESM v32 typed capture keys; Stage refuses the unaudited ESM v31 (anonymous policies) as a whole with `STAGE-ESM-016`; supported event-triggered Automation slices render, while reactions mixing event production and command invocation report `STAGE-ESM-037`, read-model absence assertions report `STAGE-ESM-027`, and unsupported v9 and v10 constructs report the codes listed in the admission table below. Reaction system identities and reaction/reducer observer filters render within their supported subset. It also refuses an evolved event until Stage can render its event-type migrations. A refused model lists every independent refusal in one render, so fixing it does not reveal new refusals one render at a time; a refusal by the target profile or an unaudited ESM version is still reported first. Validation rejects `numbers exact` (`PLAY0268`) and `implementation` attachments on concept, built-in, and whole-command rules.

Stage 4.201.1 renders a single project: the slice-local specifications compile into the application project in Debug only, and a Release build leaves them out, so there is no separate `<Project>.Specs` project. Generated C# uses `using` directives and short type names. Generated applications use central package management, `Directory.Build.props`, and a README with run instructions, with Arc 22.131.0 and Chronicle 19.99.1. Generated code files carry no file header unless you ask for one (see [File header](#file-header)).

For the embedded stdio server, the `visualize-model` MCP App, and native AI host registration, see [Screenplay MCP](screenplay-mcp.md). For Claude Desktop and ChatGPT Desktop installation, see [Screenplay desktop MCP](screenplay-desktop-mcp.md).

**Nothing needs to be running.** This is what separates the Screenplay commands from [`cratis arc`](../arc/index.md): every `arc` command talks to a *running* application over HTTP, while Screenplay generation, validation, and rendering only read files. The result is reproducible from a checkout — commit it, diff it, and run it in CI, on a machine where the application was never started.

Fetching a `.play` document from a running application over an introspection endpoint is a separate, complementary route: it trades the SDK requirement for the requirement that the application be running. Source generation remains reproducible from a restored checkout and does not execute application startup or connect to Chronicle/PostgreSQL.

## Executable model versions

A newer bundled compiler can produce a newer executable semantic model (ESM) version than the rest of the CLI was built for, so each command admits model versions explicitly rather than following the package version:

| Command | ESM v7 (generated command values, command responses and policy negation with `not`) |
|---|---|
| `cratis screenplay validate` | Admitted. Without `--executable` it parses and merges v7 syntax and runs the source checks only; with `--executable` it also binds the model and reports binding errors. |
| `cratis screenplay mcp` | Admitted for authoring and reading. The embedded server binds v7 models, pages their executable model (generated properties, responses, generation fixtures and return expectations) and reports execution readiness. The embedded server offers deterministic reference execution through `run-specifications`, including retained proposal candidates. |
| `cratis render` | Admitted. Policy negation renders. Server-generated, non-optional UUID-backed concepts render as Arc generated-value parameters. Scalar responses and named responses whose fields come from scalar command properties render. Unsupported generated types report `STAGE-ESM-028`; unsupported generation/response combinations report `STAGE-ESM-038`. Stage refuses a specification whose unauthenticated caller carries roles or claims with `STAGE-ESM-011`; this includes the unchanged canonical negation example. A refusal leaves no publishable artifacts. Versions newer than v28 are still blocked by `CLI-RENDER-004`, naming the version. |
| `cratis screenplay generate` | From Arc 22.52.0, the Arc adapter recovers supported generated UUID values and command responses by default for commands without successful scenarios. Output containing these constructs compiles to ESM v7, which `cratis render` admits. The renderer supports non-optional UUID-backed generated concepts and scalar or named scalar command responses; unsupported types or combinations still receive typed refusals. Unsupported shapes and values needed by pre-generation protection remain in code with `SP0052`. Marten and Critter Stack retain their existing recovery surface. |
| `cratis run` | The Stage 4.201.1 container compiles the mounted `.play` files with its own bundled Screenplay compiler. Passing `cratis screenplay validate` does not establish Stage support for every construct. |

The bundled Stage 4.154.1 is built on Screenplay 4.206.0 and admits ESM v18 query-error expectations (`then error` after `when query`), introduced in Screenplay 4.161.0. Supported query rejection specifications render as companion specifications asserting the validation failure; unsupported shapes report Stage diagnostics. ESM v5 adds keyed read-model absence assertions; v8 adds event sources, streams, command routes and specification routes. ESM v9 adds public events, foreign event origins, translation direction, event-target projections/reducers and event-source captures; v10 adds reaction system identity (`runsAs`), per-production route overrides (`SemanticProducedEvent.Route`) and observer filters (`SemanticReaction.From` and `SemanticReducer.From`). Version reading and command admission are separate:

| Command | ESM v8 admission | ESM v9 admission | ESM v10 admission |
|---|---|---|---|
| `cratis screenplay validate` | Admitted for source validation and `--executable` binding. | Admitted for source validation and binding; not proof of renderer or runtime support. | Admitted for source validation and binding; not proof of renderer or runtime support. |
| `cratis screenplay mcp` | Admitted for authoring, reading and binding, including event-source, stream and route views. | Admitted by the bundled Screenplay server, including reference specification execution. | Admitted by the bundled Screenplay server, including reference specification execution. |
| `cratis screenplay conform` | Admitted for structural comparison. Coverage gaps and declaration-level fallback remain explicit. | Admitted by the bundled Screenplay comparison API; a pass does not establish Stage support. | Admitted by the bundled Screenplay comparison API; a pass does not establish Stage support. |
| `cratis screenplay generate` | Checks recovered source with the bundled compiler. The Arc adapter's output remains ESM v7; recovery is provider-owned. | Does not execute or render these constructs. Recovery remains provider-owned. | Does not execute or render these constructs. Recovery remains provider-owned. |
| `cratis view` | Serves embedded or generated source through the bundled event-model explorer. | Viewing is not renderer admission. | Viewing is not renderer admission. |
| Generation statistics | Count declarations in compiled source, including documents with event routes. | Counts do not establish executable or renderer support. | Counts do not establish executable or renderer support. |
| `cratis render` | Admitted through Stage 4.154.1, including routed commands and supported routed specifications. PascalCase event-source names render as `{Name}EventSource` with public stream-name constants; colliding constants report `STAGE-ESM-012`. Unsupported constructs still block publication. | Admitted for construct-level planning. Public events and event-target projections/reducers report `STAGE-ESM-032`; foreign origins and event-source captures report `STAGE-ESM-033`; translation direction reports `STAGE-ESM-024`. | Admitted for construct-level planning. Reaction `runsAs` renders with the declared system roles; invoking a command whose authorization reads claims or uses opaque policies reports `STAGE-ESM-034`. Reaction `from` filters (`SemanticReaction.From`) render as Chronicle source/stream attributes. Per-production route overrides (`SemanticProducedEvent.Route`) report `STAGE-ESM-036`. Reducer `from` filters (`SemanticReducer.From`) render as Chronicle source/stream attributes; specifications requiring filtered read-model replay remain refused. |
| `cratis run` | Runs on `semantic` by default. The explicit `--engine eventmodel` fallback is deprecated, logs a startup warning and will be removed in Stage v6; it refuses named event sources, streams and routes with `STAGE-ESM-030`. A refusal stops the container without restarting or switching engines. When an explicitly selected EventModel engine reports an issue pointing to semantic, the suggestion gives the exact `cratis run <path> --engine semantic` command with your non-default options. See [Runtime engines](run.md#runtime-engines). | Stage applies its own runtime admission; CLI validation is not Stage admission. | Stage applies its own runtime admission; CLI validation is not Stage admission. |

Version admission does not admit every construct. A selected scope without unsupported constructs can plan normally at v9 or v10; a typed renderer refusal leaves no publishable artifacts. ESM v11 (lossless lowering of Double-mode number literals within ±(2^53−1); `numbers exact` documents still do not bind), v12 enum member numbers and v13 concept compliance are admitted by the bundled renderer. Snapshot collection queries render as one-time queries; observable collection queries return Chronicle's live read-model stream through Arc. Both support a `by` key and ESM v14 equality query filters, with optional filters narrowing only when supplied; DateTime filters remain refused with `STAGE-ESM-010`. Protected observable collection-success specifications remain refused with `STAGE-ESM-011`; denial specifications still render. Projection `all` blocks whose mappings set event-source identities render as Chronicle `FromAll`; literal-mapping or mapping-less `all` blocks remain refused with `STAGE-ESM-017`. ESM v15 concept generation strategies render with freshness scopes on generated event sources. Time-ordered factories use the wall clock, not a command-receipt timestamp. ESM v16 stored event names render: an event's `id` pin keeps its stored Chronicle event type name when you rename the event. For example, an event named `Renamed` with `id "Original"` renders with `[EventType("Original")]`. ESM v1 through v28 are admitted; ESM v29 and later versions are blocked by `CLI-RENDER-004` before target planning or publication. ESM v25 protected command reads (`command reads protected`) render as Arc read protection: each command with `reads` gets `[ReadProtection(On)]` and a read-model `Handle` parameter per read. Reads Stage cannot render faithfully, such as reads combined with constrained events or read paths in `produces when`, are still refused with `STAGE-ESM-044`, which names the reason. Generated single-instance queries keyed by a text or UUID value take an event source id; UUID keys match as text, so callers send the canonical lowercase form. `cratis run` runs ESM v27 composite read-model keys on the `semantic` engine (the default); the `eventmodel` engine refuses them. At Stage 4.154.1 composite read-model keys are refused before any artifact is planned: `STAGE-ESM-048` names each read model with two or more key parts and each projection keyed by `parts`, and `STAGE-ESM-047` names each single-instance query by key parts, because Chronicle cannot look up one read-model instance by a composite key yet. Stage refuses constructs it does not render with typed STAGE diagnostics; v22 models that declare atomic reaction batches report `STAGE-ESM-043` at Stage 4.154.1. Non-text secret concepts without personal-data classification are refused by both the renderer and Host with `STAGE-ESM-041`.

| Version | Newly admitted rendering | Refused shapes |
|---|---|---|
| ESM v17 | `then no events` renders for commands and single production-only reactions. `when redelivered <Event> to <Reaction>` renders for a single named reaction trigger with event productions or one supported command invocation. | Fan-out, deeper cascades and other unsupported specification shapes report `STAGE-ESM-011`. |
| ESM v18 | A specification that queries with an out-of-rule key or filter value and expects `then error` renders as a companion specification asserting the query's validation failure. | An optional concept key or filter with a `not empty` rule reports `STAGE-ESM-010`, because Arc skips validation for a null argument while the reference validates it. Query rejection specifications with prior events or read-model state, and composite query arguments that leave out members, report Stage diagnostics rather than rendering differently from the reference. |
| ESM v19 | Reaction invocations support `on refused`, `on refused by validation`, `on refused by constraint` (optionally naming the constraint) and `on refused by authorization`, with `produces` or `acknowledge`. Branch mappings preserve `$refusal.reason`, `$refusal.constraint` and `$refusal.message`. | Shapes Stage cannot reproduce exactly report `STAGE-ESM-039`, including ambiguous first-constraint names, localized refusal messages and refusal branches across same-source triggers. Redelivery specifications that combine with refusal branches report `STAGE-ESM-011`. |
| ESM v20 | A concept that declares `generate` may carry validation rules. The rendered concept has both its `New()` factory and its `ConceptValidator`. A rendered specification using a rule-bearing generated concept must pin its generated value. | A pinned value that fails the concept's rules is refused with `STAGE-ESM-011`. Rules of every severity render on a generated concept; a generated value that Stage cannot prove satisfies its rules must be pinned (`STAGE-ESM-038` otherwise). |

ESM v28 Uuid `==` and `!=` in command `require` conditions and `produces when` guards render as Guid comparisons, including protected read paths in `require`; Uuid literals render as `Guid.Parse(...)`. The semantic Host and specification executor compare canonical lowercase hyphenated Uuids, including concept values, and reject non-canonical input before evaluating requirements. Legacy engines refuse these comparisons with `STAGE-ESM-049`.

Stage reports unsupported conversion content with `STAGE-PROJECTION-001`–`STAGE-PROJECTION-008` and `STAGE-SCENE-ARRANGEMENT-001`–`STAGE-SCENE-ARRANGEMENT-004`; conversion errors block rendering. `cratis render` does not expose Stage's opt-in render-around mode: refusals still leave no publishable artifacts.

A described command handler (ESM v23) still refuses rendering with `STAGE-IMPL-002`. It no longer refuses an entire semantic Host model or specification run: only a command or specification that reaches the body reports Unsupported with the `ImplementationBody` capability, after authorization and validation. Semantic specification runs execute application, startup, shutdown, interval and schedule triggers; live Host runs and rendered applications still refuse them. Semantic specification runs also reject empty, whitespace-only, padded, `*`- or `#`-containing protected-read keys before looking up state.

Version admission sends these models to Stage's construct-level checks; it does not guarantee that every shape renders. A typed refusal leaves no publishable artifacts.

### Routed command example

Save this model as `Banking.play`:

```screenplay
concept AccountId : Uuid
concept Month : Int
eventsource Account
  identifier AccountId
  stream Transactions
    streamId Month
module Banking
  feature Deposits
    slice StateChange Deposit
      command Deposit
        accountId AccountId identifier
        month Month
        amount Decimal
        stream Account.Transactions
          streamId = month
        produces event Deposited
          for accountId
          amount Decimal = amount
```

```bash
cratis render Banking.play --name Banking --destination ./out
```

The model binds to ESM v8. Rendering emits `EventSources/AccountEventSource.cs` and a `Deposit` command bound to `Account.Transactions`; its event stream ID comes from `month`. A successful render is an artifact plan and publication, not a generated-application build or runtime verification.

## `cratis render [PATH]` or `cratis render --workspace <FILE>`

Compiles one `.play` file, every `.play` file beneath one folder, or the exact document set in a canonical workspace as a single logical application, then asks a statically bundled renderer target for a complete artifact plan. Authored `description` and event `documentation` are rendered as XML doc comments (`<summary>` and `<remarks>`). Metadata the renderer cannot use, such as characters that are invalid in XML, or a bundled renderer contract violation blocks rendering with `CLI-RENDER-005`. When the documents cannot be read as a Screenplay workspace, for example a file with an upper-case `.PLAY` extension, the model renders without the comments and the `CLI-RENDER-006` warning says why. New artifacts are published only after source compilation, ESM binding, execution-plan admission, target planning, and artifact validation all succeed. Without `--check`, recovery of an interrupted earlier publication still runs before new planning and can restore or clean the destination even when that planning fails.

Before publishing, every render checks existing Chronicle event type ids in the destination, at application scope and selected scopes. Publication proceeds only when Stage confirms compatibility. An id change or an existing id that cannot be read safely produces `CLI-RENDER-016` with Stage's `STAGE-EVENT-002` diagnostic and exits with 5. The message lists each path, type, and old and planned id. Pin the event's `id` to keep its stored event type name when renaming it. The refusal writes no new artifacts; `--force` does not bypass it. `--check` performs the same check without writing.

```bash
cratis render ./plays \
  --target cratis \
  --destination ./out \
  --name MyApplication
```

For plain `.play` files or folders, `--name` is required. Without persisted identities, it defines the stable application identity and defaults both the generated project name and root namespace; the destination path never does. Renaming or moving `./out` therefore does not silently rename the modeled application. `--project-name` and `--root-namespace` override rendering choices without changing the application identity, semantic revision, Chronicle event-store name, or MongoDB database name.

When the model root contains `.screenplay/identities.json`, PATH rendering opens the root's persisted workspace and renders its documents, saved application name, event contracts and type identities — the same identities Studio and the Screenplay MCP use. The model root is the selected folder, or a single `.play` file's parent directory; a persisted root supplies its complete mapped document set, even when PATH names one file. `--name` must match the saved application name; a different name reports `CLI-RENDER-018` and exits with 5, because the persisted identities belong to that application. This changes generated output for roots that previously rendered with temporary identities. Existing destination event ids still face the `CLI-RENDER-016` compatibility check; `--force` cannot bypass it. Roots without persisted identities retain their existing discovery and temporary identities. Existing applications retain their artifact bytes; new destinations use the reducer convention below.

Opening a persisted root is read-only. A pending operation, corrupt identities, unmapped source file, permission problem or other typed open refusal reports `CLI-RENDER-017` with `FailureKind: Reason` and exits with 5, without planning or publishing new artifacts. Render never falls back to temporary identities or recovers the model root automatically. Resolve the reported root problem through the Screenplay workspace workflow; do not delete identities to regenerate ids.

For example, `cratis render ./plays --name MyApplication --project-name Delivery.Backend` selects `Delivery.Backend.csproj` and `Delivery.Backend.slnx` while retaining `MyApplication` as the application identity and default root namespace.

The bundled Stage renderer applies `--root-namespace Company.MyApplication` to the generated project and C# namespaces. This remains independent of `--project-name` and does not rename the application. Rendering admits only supported backend semantics; a successful artifact plan is not a generated-application build or runtime verification. Screenplay validation accepts events with multiple declared generations, but the bundled Stage renderer does not yet render an evolved event: `cratis render` reports `STAGE-ESM-026` naming the event and its revision, because Stage cannot render the Chronicle event-type migrations it needs yet, and publishes no artifacts. Stage's direct `.play` syntax route refuses event generations with `STAGE-EVENT-001`. `cratis screenplay validate` only checks the Screenplay model, not Stage renderer support.

Navigation contributions render with everything they declare: `id`, `icon`, `presentation`, `group` and `destination outlet|dialog|external` reach the Scene the bundled Stage renderer plans, and the running application honours them - the menu shows the entry as its `presentation` asks and opens an outlet or a dialog destination through the Scene navigation host. A contribution on a module reaches every screen beneath the module, and a screen no entry lists is shown under its own module and feature. A `toolbar` renders with its items' destinations and presentation hints. (`CLI-RENDER-015`, which refused these properties before Stage 4.73.1, is retired.)

The initial `cratis` target covers the released backend vertical: concepts and composite types, a command with `not empty` validation, its event destination and mappings, a one-instance projection, an optional snapshot by-key query, and generated success/rejection specifications. Unsupported reachable semantics are blocking diagnostics, never omitted behavior or generated TODOs.

For a `.play` file or folder, `cratis render` reads referenced implementation files beneath the model root (the folder, or the single file's **parent directory**) before compiling. Use a dedicated folder for models with file attachments so unrelated files in a single file's parent directory are not eligible for loading. The bundled Screenplay compiler identifies resolved bodies by content hash; the bundled Stage renderer verifies every compiler requirement against its supplied body before planning. File attachments that escape the root, are links, missing, too large, or non-text report `PLAY0430`–`PLAY0434` warnings. If a body required by the model is missing or changed, `STAGE-ESM-020` blocks publication and includes the loader's refusal reason when the loader refused the file. Resolved C# command and concept rule bodies that return `bool` or yield validation messages render into generated validators through an app-local typed context, rather than being refused with `STAGE-ESM-005`. Interpolated messages use invariant culture. Bodies reading `Tenant`, `CausedBy` or `Occurred` report `STAGE-ESM-015`; other bodies outside Stage's pure subset remain refused. Inline `generate rule` factories require implementation requirements and resolved contents from the same Screenplay compilation. Bodied reducers and opaque `csharp`/`file` policies render when their body passes Stage's closed-compilation `pure` check; the admitted subset is listed in [admitting target semantics in the Stage renderer guide](/screenplay/stage/guides/build-renderer-target/#admit-target-semantics-before-emission). A reducer body uses the generated PascalCase members (`context.Event.Amount`, not `context.Event.amount`). A body that does not compile, or a reducer shape Stage cannot render, reports `STAGE-ESM-019`; a symbol outside the allowlist reports `STAGE-ESM-022`; a policy body that reads a `PolicyContext` member Stage does not supply reports `STAGE-ESM-015`. These are target diagnostics, not compiler errors. The canonical workspace envelope carries source documents but no filesystem attachment root: inline bodies resolve from its document text; file references without supplied contents fail with `STAGE-ESM-020` rather than being read from the workspace file's directory.

### Reducer rendering convention

A destination with no `.cratis-render.json` ownership manifest is a new application, even if the directory already contains other repository files. New applications render self-reducing read models: the read-model record contains the static `Reduce` method instead of a separate `IReducerFor<T>` class. Render publishes the choice as `"reducerConvention":"self"` in the managed `.cratis/stage/rendering.json` file.

Every later render reads that file and keeps the same convention. This applies to full renders, `--scope` (including a fresh destination's scaffold), `--workspace` and read-only `--check`. A fresh-destination check lists the configuration as a pending write without creating it. An existing application with an ownership manifest but no reducer configuration keeps classic reducers and unchanged output until you migrate a read model explicitly.

Do not edit or delete the managed configuration. Invalid JSON, an unsupported version, or a different application name reports `CLI-RENDER-021`; a missing file still owned by the manifest reports `CLI-RENDER-022`; an edited managed file reports `CLI-RENDER-023`, even with `--force`; a reducer configuration added to an existing application that did not render it reports `CLI-RENDER-024`. Both render and `--check` refuse with exit 5 and publish nothing. Restore the original file for that application before rendering again.

### Migrate a classic reducer

An existing application keeps its separate `IReducerFor<T>` reducer classes. Migrate one read model at a time to a self-reducing read model with `--migrate-reducer`, naming the read model by its dotted address `Module.Feature.Slice.ReadModel` or its semantic id. The option is repeatable and needs a whole-application render of a destination that already has an ownership manifest:

```shell
cratis render ./plays --name Shop --destination ./out --migrate-reducer Orders.Ordering.Totals.Total --check
cratis render ./plays --name Shop --destination ./out --migrate-reducer Orders.Ordering.Totals.Total
```

Render reads the managed classic reducer from the destination, and the read model keeps the reducer's observer id, so Chronicle does not replay it. The publication writes the self-reducing read model, writes version 2 of `.cratis/stage/rendering.json` recording the migration, and deletes the old reducer file in the same atomic publication: each deletion is verified against the hash taken from the file, backed up first, and restored by recovery if the publication is interrupted. `--check` lists the deletion as a pending change and exits 6 without writing. Later renders read the version 2 configuration, so the option is not needed again; a read model that is already migrated is skipped. Migration is one way. Version 1 configurations are still read. `--migrate-reducer` cannot be combined with `--scope`.

Upgrade every copy of the CLI that renders the destination, including CI, before you migrate. An older CLI does not understand the version 2 configuration and refuses every render of a migrated destination with `CLI-RENDER-021` (exit 5). An older CLI that recovers an interrupted migration also restores deleted reducers without checking their backup hash.

| Diagnostic | Meaning |
|---|---|
| `CLI-RENDER-025` | A file the migration must delete changed after the migration evidence was taken, or cannot be removed safely. Render and `--check` exit 5 and nothing is written or deleted. A file that changes during publication is kept and the rest of the publication is rolled back. A recovery whose backup of a deleted file no longer matches its verified hash refuses, restores nothing and names the journal and backup to inspect and clear. |
| `CLI-RENDER-026` | The read model cannot be migrated: `--scope` was given, no destination or ownership manifest, an unknown read model, no or several managed classic reducers for it, a reducer Stage cannot migrate, or a managed reducer that was edited (even with `--force`). Exit 5; nothing is published. |

Stage's own refusals for a reducer it cannot migrate (`STAGE-REDUCER-001` to `STAGE-REDUCER-004`) surface as render diagnostics and exit 5, like other Stage refusals.

### Render selected scopes

Use `--scope <Module>[.<Feature>…][.<Slice>]` to render an admitted part of a model while other slices still need hand-written implementation or renderer support. Names match authored names exactly, including case. A module selects all its slices; a feature includes its nested features; a slice selects only itself. Repeat the option to render a union:

```bash
cratis render ./plays --name Shop --destination ./out --scope Orders
cratis render ./plays --name Shop --destination ./out \
  --scope Orders.Placing.PlaceOrder --scope Customers.Registration
cratis render ./plays --name Shop --destination ./out --scope Orders.Placing --check
```

[Release-baseline warnings](#release-baseline-warnings) cover the whole application, including admitted event revisions outside the selected scopes.

The entire source set must compile and bind. Execution-plan (`PLAN-*`) problems in unselected slices are reported as warnings; problems in selected slices and application-level declarations still block a scoped render. When the selected module or feature no longer contains any slices, execution admission covers application-level declarations only; unrelated slice problems remain warnings, so they do not prevent removing that scope's previously owned artifacts. Renderer (`STAGE-ESM-*`) refusals in unselected slices do not block a scoped render: Stage admits and renders only the selected slices. Shared concepts and composite types referenced by those slices are included. Authored XML documentation and shared policy artifacts have the same bytes as in a full render. Unknown addresses report `CLI-RENDER-011`, including existing sibling names at the first unmatched level. An address that names both a feature and a slice reports `CLI-RENDER-012`. Both exit with 5.

The application scaffold and default Scene belong to Application scope. A scoped plan excludes both. When the destination has **no ownership manifest**, publication adds the plain application scaffold, using the same application name, project name and root namespace as a full render; it does not add a default Scene. Once a manifest exists, scoped publication leaves the existing scaffold alone.

A selected slice may reference an event, read model, command or query declared in another slice. Render that owning slice first, or include it in the same invocation. The destination manifest must already own an artifact listing the dependency's semantic id, unless the current plan generates it. Missing declarations produce `CLI-RENDER-014`, naming the declaration and the `--scope` address to add, before publication. The CLI does not widen the selection automatically. Stage supplies referenced concepts, composite types, event-source definitions and streams in the scoped plan.

Scoped publication preserves other scopes' files as managed. It deletes an absent planned artifact only when it has recorded `owners` chains and every chain contains a selected module, feature or slice id. Each chain records one source's module, feature hierarchy and slice. An artifact attributed to several slices, such as a reducer and its observed events, is kept if any chain lies outside the selection. For older manifests without owners, it deletes only artifacts with nonempty `sources` that all resolve inside the selection in the current model. Schema-1 artifacts have no sources, so a scoped render deletes none of them. Modified stale artifacts remain a refusal, even with `--force`.

Historical owners let a feature or module render remove files of a deleted slice. Artifacts with unresolved source ids and no matching owner are retained and listed as `orphaned`; a later full render removes them. This is semantic ownership, not an inference from generated file paths.

Before replacing shared policies in a scoped render, both render and `--check` verify their destination layout. An aggregate or unrecognized layout produces `CLI-RENDER-013`, names the affected paths, and asks you to render the whole application once. Migrate with a full render before using scopes; the CLI refuses rather than silently rendering more than you requested.

For incremental adoption, render an admitted feature into a fresh destination, render its admitted dependencies or additional disjoint scopes as they become available, and use `--check --scope` to detect drift without writing. Once the whole model is admitted, a full render accepts all scoped artifacts as managed. A successful scoped publication still requires the same generated-application build and test verification as a full publication.

### Canonical workspace input

```bash
cratis render --workspace ./application.workspace.json \
  --destination ./out \
  --project-name Delivery.Backend
```

For release-baseline warnings, workspace mode reads `.screenplay/release-baseline.json` beside the workspace file, or the file you select with `--baseline <FILE>`. It checks the supplied workspace's documents and identities, not `.play` files or identities beside it. See [Release-baseline warnings](#release-baseline-warnings).

`--workspace <FILE>` is an explicit file-only input mode, mutually exclusive with positional `PATH`. Without it, the existing `.play` file/folder discovery and current-directory default remain unchanged. There is no automatic JSON detection, archive extraction, or Git operation.

The CLI uses the shared `Cratis.Screenplay.Workspaces.ScreenplayWorkspaceSerializer.Deserialize(ReadOnlySpan<byte>)` API for the canonical version 1 envelope. The supplied application identity and name, exact identity catalog, document IDs, stable keys, portable display paths, and exact source bytes are authoritative. Planning receives the documents' strictly decoded text (including original line endings) and supplied identities through the real document-set compiler; it never bootstraps replacement identities from file paths. The input file is not modified or extracted.

`--name` may be omitted in workspace mode. If supplied, it must match the workspace application name exactly (ordinal comparison); it cannot rename or re-bootstrap the workspace. `--project-name` and `--root-namespace` remain independent rendering overrides, defaulting to the workspace application name. The legacy CLI single-identifier check is not an additional workspace admission rule; package-owned compiler and renderer validation still apply.

The complete envelope is limited to **32 MiB (33,554,432 bytes)**, including JSON metadata and base64 expansion. The CLI counts actual bytes while reading, probes at most one byte beyond the limit, and rejects oversize input before deserialization; a file-length snapshot is not trusted. This is a transport admission limit, not a new source-language or metadata identifier rule. Decoding, catalog validation, and compilation require additional memory/CPU; the limit is not a total process-memory quota.

Only opened regular files are admitted. Linux and macOS open nonblocking and inspect that same handle before reading, rejecting FIFOs, sockets, directories, and devices. Windows rejects device/raw namespace paths before opening and validates the opened disk handle. Symlinks may resolve to regular files; admission is not a directory sandbox. Path replacement after opening cannot redirect reading to a different file. Native inspection failure is a safe rejection, not a fallback to trusting path attributes. Linux requires the `statx` API and macOS requires `fgetattrlist`. These checks avoid the no-writer FIFO open hang; they do not promise a wall-clock deadline for arbitrary filesystems, network mounts, or drivers.

Missing files produce a not-found error. Nonregular or unreadable files, malformed/noncanonical envelopes, unsupported versions, inconsistent catalogs, revision mismatches, and oversized input produce safe validation errors, no successful render result, and no new published artifacts. Structural admission and optional name agreement happen before recovery. A structurally valid workspace with invalid-but-editable source still fails rendering with compiler diagnostics after the normal recovery step; structural admission is not semantic success. Cancellation is checked while reading and around shared deserialization and planning, and aborts without new publication. Shared synchronous compilation/deserialization cannot be interrupted mid-call; cancellation is observed immediately afterward.

Revision verification detects content inconsistency, not authenticity. Import does not authenticate a sender or enforce an independently expected revision/optimistic-concurrency token. Only transfer source content you intend the receiving host to read.

### Render options

| Option | Description |
|---|---|
| `--workspace <FILE>` | Canonical version 1 workspace envelope, at most 32 MiB. File only; mutually exclusive with positional `PATH`. |
| `--baseline <FILE>` | Release-baseline file for `--workspace` only. Overrides `.screenplay/release-baseline.json` beside the workspace file. An explicitly missing file warns; an absent discovered baseline does not. With PATH input, exits with 5. |
| `--scope <ADDRESS>` | Repeatable exact authored module, feature or slice address (`Module.Feature.Slice`). Omit to render the whole application. Works with `--check` and `--workspace`; release-baseline warnings still cover the whole application. |
| `--migrate-reducer <READMODEL>` | Repeatable read model (`Module.Feature.Slice.ReadModel` or semantic id) to migrate from a classic `IReducerFor<T>` reducer to a self-reducing read model; deletes the old reducer file with the render. Needs a whole-application render of an existing destination; cannot be combined with `--scope`. See [Migrate a classic reducer](#migrate-a-classic-reducer). |
| `--target <TARGET>` | Statically bundled renderer target. Defaults to `cratis`; arbitrary plugins cannot add executable targets. |
| `--destination <DIRECTORY>` | Managed artifact destination. Defaults to `./out`. |
| `--name <NAME>` | Required for plain source; optional with `--workspace`, where it must exactly match the supplied name. Defaults the project name and root namespace. |
| `--project-name <NAME>` | Generated project and solution name. Defaults independently to the application name. |
| `--root-namespace <NAMESPACE>` | Root namespace for generated C#. Defaults independently to the application name, not `--project-name`; does not change application identity. |
| `--package-set <FILE>` | Approved package set (JSON) naming the custom runtime packages a `ui profile` may use beyond `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default`. See [Custom runtime packages](#custom-runtime-packages). |
| `--file-header <LINE>` | Repeatable plain-text line that every generated C#, TypeScript and JavaScript file starts with, written as a `//` comment. One value may hold several lines separated by `\n`. `{year}` becomes the current UTC year. Defaults to no header. Mutually exclusive with `--file-header-file`. See [File header](#file-header). |
| `--file-header-file <FILE>` | Text file whose lines form the file header, as for `--file-header`. An empty file means no header; a missing file is reported as `not_found`. |
| `--force` | Replace a modified active file already owned by the manifest. It never authorizes an unmanaged overwrite or deletion of a modified stale file. With `--check`, evaluate that forced render without writing. |
| `--check` | Plan normally and check publication against the real destination without writing or recovering. Exit 0 when artifacts and the ownership manifest are current, 6 when writes, deletions or a manifest rewrite are pending, or 5 for planning failures, publication refusals, or pending recovery. |

Rendering overrides must be dot-separated C# identifiers without paths, empty segments, surrounding whitespace, or reserved keywords. Invalid names produce the blocking `CLI-RENDER-002` diagnostic and no artifacts are published. Plain-source `--name` retains its existing single-identifier requirement.

### File header

Generated code files carry no header by default. To start every generated C#, TypeScript and JavaScript file with your own copyright or license notice, pass the lines as plain text, without comment markers:

```bash
cratis render ./Model --name App --destination ./src/App \
  --file-header "Copyright (c) Acme Corporation {year}. All rights reserved." \
  --file-header "Licensed under the Acme license."
```

The renderer writes each line as a `//` comment, writes an empty line as `//`, and follows the header with one blank line:

```csharp
// Copyright (c) Acme Corporation 2026. All rights reserved.
// Licensed under the Acme license.

namespace App.Orders.Placing.PlaceOrder;
```

Configuration, markup and documentation files (`.csproj`, `.json`, `.md`, `.html`) never get a header. `{year}` is replaced with the current UTC year when you render. Repeat `--file-header` for each line, or separate lines inside one value with `\n`. To keep the notice in the repository instead, put it in a text file and pass `--file-header-file ./header.txt`; each line of the file is one header line.

The header is part of every generated file, so render with the same header each time. Rendering with a different header, or without one, rewrites every managed code file, and an edited managed file still needs `--force`.

### UI profile resolution

Before anything is published, every `ui profile` is resolved against the packages the bundled `cratis` target renders - `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default` - and the custom packages approved with [`--package-set`](#custom-runtime-packages). A profile input the target cannot honor stops the render with a blocking diagnostic. Nothing is published, and managed artifacts from an earlier render stay as they were.

| Diagnostic | The profile |
|---|---|
| `CLI-RENDER-007` | Activates a package that is neither one the bundled target renders nor approved with `--package-set`. For a published Scene blueprint, the message names its npm package; see [Use a published Scene blueprint](#use-a-published-scene-blueprint). |
| `CLI-RENDER-008` | Selects a layout that neither the application nor an active package declares. |
| `CLI-RENDER-009` | Uses packages that cannot be combined: its theme is not declared compatible with one of them, or a package dependency is missing, conflicting or cyclic. |
| `CLI-RENDER-010` | Selects a theme the application does not declare. |
| `CLI-RENDER-019` | Names a `root template` that cannot be the application's shell: it does not exist, its blueprint is not installed, its `version` pin is not satisfied by the installed blueprint, or it has no `content` outlet. The message names the template and, for a pin, the installed version. |
| `CLI-RENDER-020` | Not a profile input: a screen names a template of a blueprint (`template blueprint <Package> <Template>`) that the approved blueprint does not provide - no such template, a version outside the pin, or a blueprint that is not approved. Rendering it would show a screen with no template, so it stops the render, and `cratis run` before Docker starts. |

A domain blueprint (`@cratis/scene.blueprint.crm`, `.ticketing`, `.approvals`, `.inventory`, `.hr`, `.project-tracker`, `.administration`, `.orders`, `.catalog`, `.vendors`, `.invoicing`, `.customer-portal`, `.contracts`, `.work-orders`, `.bookings`) is approved with the manifest its package publishes, `blueprint` and `sampleData` members included; they are design-time only and never reach the rendered application. Its dependency on `Cratis.Blueprint.Default` is satisfied by the bundled target, and the screens that fill its templates (`template blueprint <Package> <Template>`) render in the generated React application and in `cratis run`.

A `root template` is the application's shell (Cratis/Stage#466): `cratis render` and `cratis run` both start the shell Scene's `resolveEffectiveShell` names, apply its `option`s (`navigationMode`, `menuTheme`, `showFooter`) and the profile's `appearance` (`color scheme`, `accent`), and never fall back to a different shell. The legacy `layout <Name>` line still works. `cratis run` reports the compiler's root template errors (`PLAY0790`-`PLAY0799`: an unknown template or blueprint, a template that cannot be a shell, no `content` outlet, an invalid colour scheme or accent) with their code and `file(line,column)` location, and `CLI-RENDER-019` for a pin no installed blueprint satisfies, before Docker starts.

Screenplay reports a missing layout (`PLAY0263`) and an incompatible theme as warnings about the source text, because a document that names them is still valid. A render is different: it would publish a shell or a styling that is not the one the profile asked for, so the CLI refuses instead.

### The package and image set

`cratis render` and `cratis run` resolve a model's UI the same way, because both use one Stage release: the Stage packages the CLI renders with are the Stage release `cratis run` starts as `cratis/stage:<version>`. The default tag is that release, so the rendered application and the sandbox interpret `ui profile`, layouts, templates, navigation and forms identically. `cratis version` shows the Screenplay compiler the CLI bundles; `cratis run --tag <TAG>` is for pinning another image, and a tag other than the default is on you to keep compatible with the model.

The packages a render supports out of the box are fixed by that release:

| Where | What it contains |
|---|---|
| `ui profile` packages the renderer accepts | `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default` (see [UI profile resolution](#ui-profile-resolution)), plus any package approved with [`--package-set`](#custom-runtime-packages). |
| Frontend packages in the rendered `package.json` | `@cratis/scene.*` and `@cratis/components` at the exact versions the Stage release pins (React Aria via `@cratis/components` is the default scaffold; listing `PrimeReact` under the profile's `packages` opts in to the PrimeReact scaffold, adding the PrimeReact packages and provider), with the Arc packages they need, plus the npm package of each approved custom package. |
| Container image for `cratis run` | `cratis/stage:<Stage version>`, which ships the same five packages; custom packages are handed to it with `--package-set` and loaded at runtime (see [Custom packages with `cratis run`](run.md#custom-packages)). |

A profile that names a package outside the first row stops the render with `CLI-RENDER-007` before anything is published; it never produces an application that silently lacks the package's components. The CLI does not add packages to the set on its own, and Studio is not required for either command: the profile, the packages it names and the layouts, templates and forms come from the `.play` files, and the packages beyond the shipped five come from the package set you pass.

### Custom runtime packages

A package of your own - a charts library, an in-house component set - renders once the application **approves** it. Approval is one JSON document, the Scene *approved package set*, that Scene, Stage and this CLI share:

```json
{
  "version": 1,
  "packages": [
    {
      "manifest": { "name": "Acme.Charts", "version": "1.2.0", "kind": "ComponentLibrary", "components": ["chart"] },
      "module": { "npm": "@acme/scene.charts", "version": "1.2.0", "export": "acmeChartsRuntimePackage", "styles": ["@acme/scene.charts/styles.css"] }
    }
  ]
}
```

`manifest` is the package's Scene declaration: the name a `ui profile` lists, its version, dependencies and the components, layouts, templates and themes it declares. `module` says where the runtime comes from: the npm package (and version, range or `file:` path) the frontend installs, the named export of that package that is its runtime bundle, and the package-relative style sheets to import. Use the package in the model like any other:

```screenplay
ui profile Desktop
  target platform web
  layout AppShell
  packages
    core
    Cratis.Components
    Acme.Charts

screen WorkItemList
  component Acme.Charts.chart salesChart
    property title = "Sales by week"
```

```bash
cratis render ./Model --name App --destination ./src/App --package-set ./packages.json
cd ./src/App
npm install && npm run build
```

What the render does with the set, using Stage's planner and Scene's resolution rather than a second copy of either:

- The profile is resolved against the shipped packages and the approved ones together, so an approved package's layouts, themes, dependencies and version ranges are checked exactly like a shipped package's (`CLI-RENDER-007` to `CLI-RENDER-010`), and the templates the screens name are checked against the manifest of the blueprint they name (`CLI-RENDER-020`).
- The set is refused before anything is published when it is malformed, names a package the target already ships, reuses an npm package the application already installs, lists an icon library, or has a style sheet that is a URL or belongs to another package. The error says which package and field.
- The published application gets `approved-packages.json` (the canonical set), one extra `package.json` dependency for each package, and `.frontend/approvedPackages.ts`, which imports each approved module by name and loads it through Scene's `loadApprovedPackages`. The application loads the packages before the first render and refuses to render, with the reasons on the page, when a package fails to import, lacks its export, or differs from its declaration - a profile is never rendered with a package silently missing.
- The set is part of what you ask for, like `--name`: render again without `--package-set` and a profile that names `Acme.Charts` is refused with `CLI-RENDER-007` before anything is changed.

The same set drives `cratis run`: the live Stage loads the approved packages at runtime from their browser builds, so a model that uses custom packages can be reviewed without rendering it (see [Custom packages with `cratis run`](run.md#custom-packages)). A package for a live Stage is a browser ES module that leaves `react` external; the rendered application installs the same package from npm and bundles it.

#### Use a published Scene blueprint

Scene publishes domain blueprints (`Cratis.Blueprint.<Name>`) as npm packages named `@cratis/scene.blueprint.<name>`. Only `Cratis.Blueprint.Default` ships with the renderer, so any other blueprint needs approval. For `Cratis.Blueprint.Invoicing`, install the package, write the approved package set from the manifest it exports, and pass it to the render:

```shell
npm install @cratis/scene.blueprint.invoicing@4.71.0
node -e "const p='@cratis/scene.blueprint.invoicing',m=require(p),k=Object.keys(m).find(k=>k.endsWith('BlueprintManifest'));require('fs').writeFileSync('approved-packages.json',JSON.stringify({version:1,packages:[{manifest:m[k],module:{npm:p,version:require(p+'/package.json').version,export:k.slice(0,-8),styles:[]}}]},null,2)+'\n')"
cratis render ./Model --name Invoicing --destination ./src/Invoicing --package-set ./approved-packages.json
```

The Node command reads the manifest the package exports, so the set describes exactly the installed version. To review the model live, run `cratis run ./Model --package-set ./approved-packages.json --packages ./node_modules`.

### Launch the rendered application

A render publishes a complete application: a generated .NET backend and a React frontend served by it. To run it:

```bash
cratis render ./Model --name App --destination ./src/App
cd ./src/App
docker compose up -d                      # the Chronicle the application is configured for
dotnet build App.csproj --configuration Debug   # also writes the TypeScript proxies the frontend imports
npm install
npm run build                             # writes wwwroot/
dotnet run --project App.csproj
```

The backend serves the built frontend, the generated command and query API, and `/healthz`. The CLI's own CI performs these steps on the canonical screen-composition corpus with the packed CLI and runs the Stage generated-React browser scenarios against it (deep links, forms, guarded actions, outlets and native controls), so the rendered application is held to the assertions the live Stage runtime is.

### The application shell

A model that authors `navigation topbar` / `navigation sidebar` regions (with `inherits`, `extends` and `overrides` per module), toolbar `button`s that name a State Change `slice`, and `sample data` renders them into the application's Scene document and the generated frontend, which draws them with Scene's navigation regions and task-dialog host - the components the Studio preview and `cratis run` use. Compiler errors about them are the usual Screenplay diagnostics with their code and location (`PLAY0709`-`PLAY0715` sample data, `PLAY0740`-`PLAY0752` navigation, `PLAY0760`-`PLAY0765` toolbar buttons) and stop the render before anything is written.

**Sample data never ships.** It is design and preview time only (spec SD6): the `scene.json` a render publishes - a content file the .NET SDK would copy into the publish folder - and the Scene document the frontend bundles carry no sample data pack, no blueprint pack and no profile selection. `cratis render` has no sample-data option. For a disposable semantic sandbox only, [`cratis run --sample-data [PACK]`](run.md#sample-data-in-the-sandbox) derives ordinary seed events from user-authored packs and scalar projection mappings. Without the flag, packs do not populate runtime queries. Collection properties remain empty with a warning pointing to Cratis/Screenplay#1000; this is not collection sample-data support and does not change production artifacts or seed production data.

The CLI's CI renders a fixture shell (`Integration/Cli/Shell`) with the packed CLI, builds and launches it, and drives the generated application and a live `cratis run` of the same model with one browser scenario: three modules that inherit, override and extend the sidebar, a toolbar button that opens its slice as a dialog and posts the command, deep links and back navigation, the profile's root template, shell options and appearance (own and blueprint roots, and a version pin no blueprint satisfies), and no sample row in anything that ships.

### Managed publication and recovery

The destination's `.cratis-render.json` manifest uses schema version `"2"` and records the semantic revision, application identity, target/profile/renderer/schema versions, and every managed artifact path, hash, and `sources` array of semantic id strings. Addresses are derived from the current model, not stored in the manifest. An additive `owners` array records one ancestry chain per resolvable source: `[moduleId, featureId…, sliceId]`. Chains are deduplicated and sorted; application-level artifacts contribute no chain. Older schema-2 manifests remain readable without this field. Generated artifact bytes and hashes are unchanged by this metadata.

Schema-1 manifests remain readable, with missing sources treated as empty. The next successful render upgrades the manifest to schema 2 even when every artifact is unchanged. Until that one-time rewrite, `--check` exits 6; afterward an unchanged check exits 0. Older CLIs refuse to publish over a schema-2 destination. Other schema or identity mismatches still require an explicit migration.

On a later render, the CLI:

- rejects an unmanaged file at a planned path, even when `--force` is set;
- rejects a user-modified managed file being replaced unless it remains active and `--force` is set; scoped renders leave retained files outside the selection untouched, including modified files;
- removes a stale managed file only when its bytes still match the prior manifest; in a scoped render, only files owned entirely by the selection can become stale, while other files remain managed and unresolved historical sources are reported as `orphaned`;
- rejects unknown manifest, artifact-plan, target, or renderer identity/schema changes that require migration;
- stages every new byte and records the intended operations and prior manifest in a durable journal;
- backs up files before replacement or removal and publishes the new manifest last.

If a process stops during the commit, the next invocation without `--check` that passes input admission reads the journal before planning new output. It either finishes cleanup after a published manifest or restores the prior file bytes exactly and the prior manifest content. Recovery writes that decoded manifest content as UTF-8, so it does not preserve the manifest's original encoding or byte-order mark. Cancellation before the journaled commit leaves no generated artifact behind.

A successful unchanged rerender writes no artifacts and preserves an already canonical manifest byte for byte. With JSON output, the command retains target, destination, application name, document/artifact counts, written/removed/unchanged counts, and whether recovery ran.

### Unexpected renderer failures

| Diagnostic | Meaning |
|---|---|
| `CLI-RENDER-027` | The renderer failed unexpectedly during planning. The message identifies the phase, exception type, and message. Nothing was planned or published. Report this to Cratis/Stage. |

Unexpected failures exit with `10` (`unexpected_error`), not `5` (`validation_error`). Normal output omits stack traces; rerun with `--debug` to include the original exception trace. JSON output includes the render diagnostic and the machine-readable `unexpected_error` code.

### Check rendered artifacts in CI

Use `--check` when generated code is committed beside your model and CI must detect drift without changing the checkout:

```bash
cratis render ./Model --name App --destination ./src/App --check
cratis screenplay baseline check ./Model
```

Pair the publication check with `screenplay baseline check`: release-baseline warnings from render do not fail CI when an admitted event revision changes or disappears. The baseline check exits with 1 for those violations.

The command runs the same compilation, diagnostics, and artifact planning as a normal render, then checks ownership and bytes at the real destination. It writes nothing: no destination directory, ownership manifest, `.cratis-render/` control directory, journal, staging, or backups. Existing files and directories remain untouched, including interrupted publication state.

| Exit code | Meaning with `--check` |
|---|---|
| `0` | No artifact writes, deletions, or refusals; the rendered artifacts are current. |
| `6` (`changes_pending`) | A real render would write or delete artifacts or rewrite the ownership manifest, with no refusals. |
| `5` (`validation_error`) | Planning failed, publication was refused, or an interrupted publication needs recovery. |
| `1` (`not_found`) | The input is missing or contains no `.play` documents. |
| `10` (`unexpected_error`) | An unexpected failure occurred, including a renderer planning failure (`CLI-RENDER-027`). |

JSON output keeps the target, destination, application name, document/artifact counts, and `written`, `removed`, and `unchanged` counts. These counts describe what **would** happen, not writes performed. It adds `refused` and `recoveryPending`; `publication.status` is `"check"`, and `publication.changes` includes each artifact path, not only changed paths:

| `publication.changes[].kind` | Meaning |
|---|---|
| `"write"` | A new artifact or a managed artifact whose bytes would be replaced. |
| `"delete"` | A stale managed artifact still matching its prior ownership hash. |
| `"unchanged"` | A planned artifact already matching its hash. |
| `"refused"` | A path a real render cannot safely publish, with its exact refusal `reason`. |

Refusals name directory collisions, unmanaged files, user-modified managed files, modified stale files, reserved or case-colliding paths, and unsafe paths. Manifest schema or identity conflicts are destination-level refusals with no `path`. All detected refusals are reported rather than stopping at the first. Planning failures emit the normal diagnostics and no receipt.

The check receipt uses the publication receipt's `schemaVersion`, path, and hash fields. For an unchanged artifact, the before and after hashes match. A refused path may omit hashes that could not be observed safely. The separate `manifest` hashes describe the prior bytes and proposed manifest, never a manifest written by the check. A real render rewrites the manifest whenever its bytes differ, so a manifest-only difference (for example after a CLI or renderer upgrade, or a non-canonical manifest) also exits 6.

With `--force --check`, a modified active managed file is evaluated as a write instead of a refusal. This never changes the file and still refuses unmanaged overwrites and modified stale deletions. Pending recovery is always a refusal: run render without `--check` to recover it. Table output lists verdicts and refusal reasons; quiet output emits no receipt or summary and communicates the verdict through the exit code.

### Release-baseline warnings

Rendering reports changes to admitted event revisions as warnings, in both normal publication and `--check`. Text output groups them under `warnings` on standard error; JSON output includes them in the standard-error `diagnostics` array. Standard-output results, artifact bytes, publication receipts and exit codes are unchanged.

| Code | Meaning |
|---|---|
| `STAGE-BASELINE-001` | An admitted event revision's stored schema changed. The warning names the event, contract, revision, release and first differing path. |
| `STAGE-BASELINE-002` | An admitted event revision is missing from the current model. |
| `STAGE-BASELINE-003` | The baseline is unreadable, malformed, unsupported, too large, or belongs to another application. An explicitly selected file that does not exist also produces this warning. |
| `STAGE-BASELINE-004` | Checking is unavailable, for example because a PATH baseline has no persisted identities or a supplied workspace lacks authoritative assignments. |

For **PATH input**, the baseline comes from `<model-root>/.screenplay/release-baseline.json`, read while opening the persisted root. Stage assesses the same workspace compilation that render uses, so the assessment revision matches the rendered revision; it does not check a separate reread of the source or stamp a temporary-identity revision onto the result. A missing baseline produces no warning. Without persisted identities, an existing baseline still produces `STAGE-BASELINE-004` rather than creating or reconciling identities. A persisted-root open failure is the blocking `CLI-RENDER-017` error, not an optional baseline warning. `--baseline` is not supported with PATH input: choose workspace input to supply a standalone baseline.

For **workspace input**, `--baseline <FILE>` takes precedence over `.screenplay/release-baseline.json` in the workspace file's directory. The assessment uses the exact compiled workspace documents and identity catalog. Baseline files are limited to 4 MiB and must be regular files. A missing implicitly discovered baseline produces no warning or error in either input mode.

Warnings cover the **whole application**, even with `--scope`. They appear only when planning reaches the renderer; an earlier compilation, scope or execution-plan failure does not produce a baseline assessment.

Use [`cratis screenplay baseline check`](#cratis-screenplay-baseline) as the CI gate: it exits with 1 for admitted-revision violations and 2 when checking cannot run. Render warnings do not waive the independent `CLI-RENDER-016` publication refusal for changed or unreadable Chronicle event type ids, and that refusal does not replace the baseline gate.

### Publication receipts

JSON output also contains a versioned `publication` receipt. It describes successful **filesystem publication**, not a Git commit or push. The existing human-readable panel and quiet destination output are unchanged.

| Receipt field | Meaning |
|---|---|
| `schemaVersion` | The string `"2"`, for published and check receipts. |
| `status` | `"published"`, returned only after the manifest is written and control-state cleanup succeeds. |
| `changes` | Actual prepared artifact operations: writes in next-plan order, then stale deletions in prior-manifest order. |
| `changes[].path` | Destination-relative artifact path. |
| `changes[].kind` | Stable string `"write"` or `"delete"`. |
| `changes[].beforeSha256` | Hash observed before publication, including user-modified bytes replaced with `--force`. Omitted for a newly created file. |
| `changes[].afterSha256` | Planned byte hash for a write. Omitted for a deletion. |
| `changes[].sources` | Semantic declarations realized by the artifact, in plan source order: `{id, kind, address}`. Scaffold files have `[]`. Deletes and refused stale files use the prior manifest's ids. |
| `scope` | Scoped receipts only: resolved `{address, kind}` entries, with dot-separated authored addresses and kinds `module`, `feature` or `slice`. Omitted on unscoped receipts. |
| `outsideScope` | Scoped receipts only: dot-separated addresses of slices not rendered, sorted ordinally. No full plan is computed, so this does not include their blocking diagnostics. |
| `orphaned` | Scoped receipts only: retained managed paths whose historical sources no longer resolve and whose owners do not match the selection. |
| `bySource` | Reverse view of the receipt's artifact entries: `{id, kind, address, paths}`, sorted ordinally by address, then id, with unknown addresses last. Paths are distinct and sorted ordinally. |
| `manifest.path` | The separate ownership file, `.cratis-render.json`. |
| `manifest.baseSha256` | Hash of the exact raw prior manifest bytes, including original formatting and encoding. Omitted when no manifest existed. |
| `manifest.sha256` | Hash of the exact UTF-8 manifest bytes written by this publication. |

A source address is `Module/Feature/…/Slice/Name` for a declaration in a slice, `Module/Feature/…/Slice` for a slice, and `Name` for an application-level concept or type. Module and feature addresses include their parents; the application id uses the application name. Kinds include `application`, `command`, `event`, `readmodel`, `projection`, `query`, `specification`, `concept`, `type`, `slice`, `feature` and `module`. Policies, reducers and constraints have no separate ids in the bundled model; their files carry the command, read-model, event or slice ids Stage attributes to them. Scoped receipts additionally resolve `eventsource` and `stream` sources. An id absent from the current model is retained with `kind: "unknown"` and no `address`.

For example, this illustrative receipt excerpt maps a command file to its model declaration (hash fields omitted):

```json
{
  "schemaVersion": "2",
  "status": "published",
  "changes": [{
    "path": "Projects/Registration/RegisterProject/RegisterProject.cs",
    "kind": "write",
    "sources": [{
      "id": "sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
      "kind": "command",
      "address": "Projects/Registration/RegisterProject/RegisterProject"
    }]
  }],
  "bySource": [{
    "id": "sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "kind": "command",
    "address": "Projects/Registration/RegisterProject/RegisterProject",
    "paths": ["Projects/Registration/RegisterProject/RegisterProject.cs"]
  }]
}
```

Published receipts group only changed artifacts; check receipts also group unchanged and refused artifact entries. Destination-level refusals have empty sources and no reverse entry.

An omitted hash means the path was absent at that observed boundary, not that its hash is unknown. Unchanged artifacts and already-missing stale files do not appear in `changes`. The manifest can change while `changes` is empty—for example, when only recorded metadata or manifest formatting changes—so consumers must inspect its separate hashes too. The `.cratis-render/` journal, staging, and backups are not publication paths to stage in Git.

The top-level `recovered` flag includes recovery before planning and recovery inside publication. A receipt does **not** reconstruct changes made by recovery. Automated Git delivery must stop for reconciliation when `recovered` is true, including when a later planning failure produces no receipt.

Use exclusive access to the destination while orchestrating recovery, publication, verification, and Git operations. These receipts are not a concurrent-writer protocol, repository-HEAD compare-and-swap, or a Git diff. A future Git step must verify the expected base and current hashes, and stage only its approved artifact paths and changed ownership manifest. This command does not create branches, commits, pushes, or pull requests.

## `cratis screenplay generate [PATH]`

Reads a solution or project, discovers the source providers bundled with this CLI, derives the event model from the framework artifacts and conventions they recognize, and writes a Screenplay document. The bundled providers currently cover Arc, Marten, and Marten + Wolverine (Critter Stack). The CLI owns discovery and orchestration; each provider package owns its framework semantics.

Auto detection chooses the most specific matching provider. For example, Critter Stack supersedes its Marten foundation when both Marten and Wolverine are present. If unrelated providers match, the CLI reports the candidates rather than guessing. Use `--provider` as an explicit override for mixed applications and reproducible CI.

### Adapter composition architecture (decision, not implementation)

The current CLI has a **one-provider model**. It matches one allowlisted provider for the whole application scope, asks that provider to select projects, and invokes one complete generator facade. The allowlist is compiled into the CLI; package restore, application output, configuration, and arbitrary plugins cannot add executable adapters. This compile-time boundary is the trust model for source interpretation.

The current provider name does not always identify the adapter set that executes. Both `marten` and `critter-stack` invoke the complete `Cratis.CritterStack.Screenplay` facade, whose parameterless composition includes the Critter Stack and Vogen adapters. Conversely, the Arc facade does not compose the cross-cutting Vogen adapter. Provenance can therefore report `marten` while Wolverine evidence remains in scope and Vogen facts are generated, or report `arc` while Vogen package, assembly, and capability evidence is visible but Vogen facts are omitted. The resolved Generation graph contains adapter identities, but the CLI facade currently discards that graph and reports only provider-level provenance.

The target architecture is an **atomic adapter roster** selected across the application scope. Framework profiles such as `marten` and `critter-stack` will select allowlisted adapters only; they will not own complete generation facades. Cross-cutting adapters such as Vogen can then compose with Arc, Marten, or Critter Stack before one neutral resolution and lowering pass. This target is a decision for the next production increments, not behavior implemented by this CLI version.

Until that work lands, explicit `--provider` is selection-only in name but not in execution semantics: `--provider marten` bypasses auto detection, yet it neither removes Wolverine evidence nor disables the Vogen adapter inside the Critter Stack facade. Do not use it as proof of adapter isolation. The migration is tracked by [Screenplay.Generation #17](https://github.com/Cratis/Screenplay.Generation/issues/17), [CLI #87](https://github.com/Cratis/CLI/issues/87), and the [Critter Stack roadmap #29](https://github.com/Cratis/Screenplay.CritterStack/issues/29).

By default the document goes to standard output, so it composes with the shell:

```bash
cratis screenplay generate > MyApp.play
```

Pass `--file` to write it directly instead. The output is written as raw UTF-8, byte for byte, ending in exactly one newline — regenerating an unchanged model produces an identical file.

### Arguments

| Argument | Description |
|---|---|
| `PATH` | Solution (`.slnx`, `.sln`, `.slnf`), project (`.csproj`), or folder to read. Defaults to the current directory. |

### Options

| Option | Description |
|---|---|
| `--file <FILE>` | File to write the generated Screenplay to. Writes to standard output when not given. |
| `--provider <PROVIDER>` | Source provider: `auto`, `arc`, `marten`, or `critter-stack`. Defaults to auto detection. |
| `--framework <TFM>` | Target framework to load from every multi-targeted application project, such as `net9.0`. Required when any application project targets several frameworks. |
| `--domain <NAME>` | Name of the domain the generated document belongs to. Defaults to the assembly or root namespace of the project, and to the solution name when several projects are read. |
| `--feature-root <PATH>` | Project-relative folder beneath which feature and slice placement is derived. Marten and Critter Stack apply it; Arc reports `CLI0014` and leaves it unapplied. |
| `--module <NAME>` | Name of the module every discovered feature is placed within. Defaults to the domain. |
| `--skip-segments <COUNT>` | Number of leading namespace segments to skip when inferring features and slices. |
| `--modules-from-namespace-roots` | With the Arc provider, name each feature's module after the outermost namespace segment. Marten/Critter Stack currently report `CLI0014` and leave this option unapplied. |
| `--authoring-only-constructs` | Opt in to Arc constructs not yet executable: operations and systems, event-source/stream routes, and reads and requirements recovered from `Provide()`. Defaults to false. Intended for extraction and review; these documents have no executable model (`PLAY0268`). Marten/Critter Stack report `CLI0014` and leave it unapplied. |

The output file uses `--file` rather than `-o`, because `-o/--output` is the global output *format* flag — see [Global Options](global-options.md).

```bash
cratis screenplay generate
cratis screenplay generate ./MyApp.slnx --file MyApp.play
cratis screenplay generate ./Source/MyApp/MyApp.csproj
cratis screenplay generate ./MyApp.slnx --framework net9.0 --file MyApp.play
cratis screenplay generate ./Banking.csproj --provider marten --file Banking.play
cratis screenplay generate ./Helpdesk.csproj --provider critter-stack --file Helpdesk.play
cratis screenplay generate --domain Library --module Lending --file Library.play
```

### Including authoring-only constructs

For a fuller Arc document to extract and review, opt in explicitly:

```bash
cratis screenplay generate ./MyApp.csproj --provider arc --authoring-only-constructs --file MyApp.play
```

This adds readable operations with their external systems and implementation files, event-source and stream declarations with command routes, and keyed reads and acceptance requirements recovered from `Provide()` and `Handle()`. It also retains handler references for response-only commands. Unsupported or ambiguous source still produces diagnostics instead of guessed behavior. Arc diagnostics that mention `ScreenplayOptions.AuthoringOnlyConstructs` refer to this flag.

The generated document still compiles, but while authoring-only constructs are present it has **no executable model** (`PLAY0268`). Legacy reads also report `PLAY0271`: they do not imply decision consistency. The flag does not execute operations, generate implementation code, or bypass executable-model admission. Leave it unset when you do not need these additional constructs; it defaults to false. Marten and Critter Stack report `CLI0014` rather than silently applying an Arc-only option.

Supported generated UUID values and responses do not need this flag. From Arc 22.52.0, output containing them compiles to ESM v7, which `cratis render` admits. Non-optional UUID-backed generated concepts render as Arc generated-value parameters; scalar and named scalar responses from command properties also render. Unsupported generation/response combinations remain blocking diagnostics. Arc emits them by default where admitted; commands with successful scenarios retain their legacy representation until deterministic generation fixtures and response expectations can be recovered. See [Arc's generator reference](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/generating-a-screenplay.md) for the supported shapes and diagnostics.

### Naming the modules

A document places every discovered feature in one module, named after the domain. That is right for an application that *is* one module, and wrong for one whose namespaces already say what its modules are — `Library.Authors`, `Library.Inventory`, `Library.Lending` come back as a single `module Library` holding three features.

`--modules-from-namespace-roots` takes the module of each feature from the outermost segment of its namespace instead. When every slice shares a root namespace — as they do above — that outermost segment is the root, which names one module again, so pair it with `--skip-segments` to move the modules down to the segment that tells them apart:

```bash
cratis screenplay generate --modules-from-namespace-roots --skip-segments 1
```

```text
module Authors
module Inventory
module Lending
```

Naming a module with `--module` still collapses the document into that one, whichever of these is passed. Namespace-root module inference currently belongs to the Arc provider. Marten and Critter Stack generation reports `CLI0014` rather than silently pretending to apply it.

### Finding the solution or project

When `PATH` is a solution or project file, that file is read. When it is a folder — or is omitted entirely — the CLI looks in that folder and then in each parent folder in turn, stopping at the first one that holds a match. Within a folder it prefers `.slnx`, then `.sln`, then `.slnf`, then `.csproj`. Two candidates of the same kind in one folder is reported rather than guessed at.

A solution filter (`.slnf`) is read as the solution it filters, which is how a repository holding more than one application points at the one to describe.

### Which projects take part

A Screenplay describes one application, and an application is regularly split across several projects — an executable alongside the libraries holding its slices. Every project of a solution therefore takes part in the same document, except:

- **With the Arc provider, projects that cannot declare an Arc/Chronicle artifact.** A Roslyn analyzer, build-time tool, or code-generation project resolving neither framework is left out. Marten/Wolverine contracts are frequently markerless and may live in referenced projects without a direct package reference, so Critter Stack analysis retains non-spec C# projects and lets the provider contribute only evidence it recognizes.
- **Spec projects**, by name: the ones called, or ending in, `.Specs`, `.Specifications`, `.Tests`, `.Test`, `.IntegrationTests`, or `.Specs.AppHost`. Nothing about what a spec project can see tells it apart — it references the same framework the application does — so the name is what decides. `.Specs.AppHost` covers the host integration specs start the application in.

A project that targets several frameworks must be selected explicitly. The workspace opens it once per target framework and names the results `MyApp(net10.0)`, `MyApp(net9.0)`. Without `--framework`, the CLI fails with `CLI0015`, names the base project and its available target frameworks in stable order, and asks you to select one. It never silently picks a variant.

`--framework` applies to every multi-targeted application project in the solution and matches the decorated workspace variant exactly, case-insensitively. If any such project does not offer the requested target, the CLI fails with `CLI0016` and lists that project's available targets. Projects that have only one workspace variant remain part of the application regardless of this option, so ordinary single-target dependency projects are not discarded. Spec projects are excluded before target-framework selection.

Pass a `.csproj` to select that project as the application root. The CLI also loads its deterministic transitive C# `ProjectReference` closure. It does not admit spec/test projects, unrelated projects, or projects that only reference the root in reverse. The closure is ordered by relocation-safe logical project path, project name, and target framework; a multi-targeted project still requires `--framework`.

For a direct project target, the CLI establishes one trusted workspace boundary after selecting the exact root target-framework variant and following only that variant's transitive `ProjectReference` graph. It prefers the nearest ancestor containing a `.git` directory or worktree `.git` file, which lets a host nested beneath the repository root reference sibling application projects. Outside-repository projects and authored documents fail with `CLI0017` before provider interpretation. Documents a restored NuGet package contributes are not authored source and are left out of this check: `contentFiles`, and `Compile` items a package's build targets add from inside the package, such as the entry point `Microsoft.NET.Test.Sdk` generates. An application rendered by `cratis render` references that package for its specs and therefore recovers through `cratis screenplay generate`.

When no `.git` marker exists, the boundary is the canonical common ancestor of every retained project-file directory. The CLI rejects an empty boundary or one broadened to the filesystem root with `CLI0017`; it never treats the whole volume as a workspace. The same resolved boundary defines logical ordering, project identity, source mapping, and compilation inputs. A closure containing several projects uses workspace-relative source display paths without becoming a solution for provider host-ambiguity or filtering rules. Physical boundary paths remain internal and are not emitted in provenance or generated output.

The projects that were read are named in the result, so you can see what the document covers:

```text
Projects:    Library.Api, Library.Domain, Library.ReadModels
```

### Diagnostics

Anything the generator cannot express in Screenplay is reported rather than silently dropped — a projection operator with no counterpart, a validator rule that has no equivalent, a construct only available as compiled metadata because it lives in a referenced package.

Diagnostics always go to **standard error**, grouped by severity with errors first, so redirecting standard output to a `.play` file never mixes them in:

```text
errors (1):
  error SP0203: [Library.Lending.Reserving] projection uses $combine, which has no Screenplay counterpart

warnings (2):
  warning SP0110: [Library.Authors] command handler body is not available in this compilation
  warning SP0141: [Library.Authors.Registration] validator rule Must() cannot be expressed
```

With `-o json` or `-o json-compact`, standard error is one JSON object containing both source provenance and diagnostics. Marten and Critter Stack shared-placement diagnostics also retain their typed `subject` and `outcome` fields; the CLI does not flatten their `DOTNETSP####` conflicts or unsupported outcomes into untyped messages.

**Warnings and information do not fail the command** — the document is still written. **An error does**: standard-output generation writes no `.play` bytes and exits with a validation error. When `--file` is explicit, the CLI intentionally writes the generated or partial document to that file for review while diagnostics remain on standard error; it never mixes partial source into standard output.

### Source and compatibility provenance

Target-framework selection is a CLI-owned boundary before provider matching, compatibility admission, or source interpretation. Only after every multi-targeted project resolves to one variant does the CLI collect package, assembly, capability, and target-framework provenance. `CLI0015` and `CLI0016` therefore produce no provider-generated source or provenance; the command exits with a validation error and standard output stays empty.

Every successful provider selection reports provenance on standard error, separately from the `.play` document on standard output. The report names:

- the selected provider and bundled provider package version;
- every selected project and target framework;
- each project's relocation-safe source-path policy: logical workspace-relative project path, stable project identity, policy version, display root, and ordinal case policy;
- each project's explicit role and source-structure policy: policy version, optional feature root, optional module, and skipped namespace-segment count;
- resolved Marten/Wolverine NuGet package IDs and versions, plus the analyzed application's optional `Vogen` package, from that target's `project.assets.json`;
- referenced framework assembly identities and versions as corroboration, including `Vogen` when the application references it;
- exact metadata capability fingerprints found by Roslyn, including `vogen.value-object` when both value-object attribute shapes are available.

For Marten and Critter Stack, it also reports four independent compatibility dimensions:

- **Support tier** — `Canonical`, `SourceReviewed`, `RecognizedWithLoss`, `Unknown`, or `Unsupported` package/API evidence.
- **Recognition status** — whether the provider recognized the framework generation.
- **Semantic conformance** — whether static interpretation completed and still requires human review, found contradictory evidence, or was not evaluated.
- **Lowering fidelity** — whether no loss was reported, loss was reported, lowering failed, or lowering was not evaluated.

These values deliberately do not imply one another. A canonical package set can still use behavior outside its fixture assertions, require human review, and report lowering loss. An assembly version corroborates a NuGet version but never replaces it.

```text
source compatibility:
  provider: critter-stack 0.23.0
  project: Helpdesk.Api (net9.0)
    packages: Marten 9.29.0, Vogen 8.0.7, WolverineFx 6.29.2
    assemblies: Marten 9.29.0.0, Vogen 8.0.7.0, Wolverine 6.29.2.0
    capabilities: marten.event-projection, vogen.value-object, wolverine.handler-attribute
    logical project: Source/Helpdesk.Api/Helpdesk.Api.csproj
    project identity: Source/Helpdesk.Api/Helpdesk.Api
    source policy: version 1, Workspace display root, Ordinal case policy
    project role: Application
    source structure: version 1, feature root Features, module Lending, 1 namespace segments skipped
  support tier: Canonical
  recognition: Recognized
  semantic conformance: RequiresHumanReview
  lowering fidelity: LossReported
```

`Canonical` means the bundled provider version passes the exact pinned package set and its fixture assertions — not that every API in that package combination is implemented. A package set that is canonical for a newer adapter remains `SourceReviewed` when the CLI bundles an older provider. `SourceReviewed` otherwise means the major-generation source and metadata were reviewed, but the exact provider/package combination is not canonical. `RecognizedWithLoss` means the API is identified but its source semantics cannot be interpreted exactly. `Unknown` and `Unsupported` fail closed before source interpretation.

The CLI bundles `Cratis.CritterStack.Screenplay` and the separate `Cratis.Screenplay.Generation.DotNet.Vogen` source adapter; the analyzed application owns its own optional `Vogen` package. The bundled Cratis adapter is therefore never target-application Vogen evidence. Vogen 8.0.7 is the exact canonical Vogen baseline when paired with the pinned Marten 9.29.0 and WolverineFx 6.29.2 fixture. Another well-formed Vogen 8.x is `SourceReviewed`. Missing, malformed, or divergent package evidence is `Unknown` when Vogen assembly or capability evidence is present; Vogen majors newer than 8 are `Unsupported`, while other majors remain `Unknown` until reviewed. A newer Marten or Wolverine major likewise remains unsupported until source review and canonical evidence exist.

Generated Vogen members only corroborate an authored partial value-object declaration; they never establish one. The adapter does not infer identity from a `Guid` backing, an `Id`-shaped name, or generated members. Provider version, package recognition, semantic conformance, and lowering fidelity remain independent dimensions. In particular, a `VOG` diagnostic reports lowering loss without changing a canonical support tier or claiming semantic equivalence.

Arc reports provider, target-framework, package, assembly, capability, and source-policy provenance but continues to use its existing adapter compatibility contract rather than the Critter Stack support-tier matrix.

Source-policy and source-structure provenance never include the physical checkout root, absolute project path, physical source root, or Roslyn syntax-tree path. A direct one-project generation displays source locations relative to the project; solutions and direct multi-project closures display them relative to the trusted workspace boundary. Stable file identity uses the logical boundary-relative project path without `.csproj` plus the document's project-relative logical path. Command, option, and loader diagnostics identify the target by its filename. Compiler diagnostics use the aligned source context's display path or stable file identity; an unmapped compiler location falls back to the logical project identity rather than Roslyn's physical path. These identities keep provenance, diagnostics, and generated source locations identical after moving a checkout.

### Strict shared source placement

Workspace-loaded projects carry authoritative authored syntax trees, an explicit `Application` role, and stable source contexts into the shared .NET placement resolver. That context makes placement strict: `--feature-root` is validated as a non-rooted, non-traversing project-relative path and normalized to the resolver's portable separator form before Marten or Critter Stack generation and provenance; `--module` and `--skip-segments` retain their requested values. An invalid feature root fails before provider generation with `DOTNETSP0002`, a non-disclosing message, no provenance, and no generated document bytes. Valid folder and namespace evidence must agree. Other `DOTNETSP####` errors remain blocking generation diagnostics; the CLI does not retry without context or silently apply another placement.

Compatibility has two narrow boundaries. Context-free in-memory or legacy callers retain the released provider's legacy behavior. For a context-bearing Critter Stack project, only a strict failure consisting solely of `DOTNETSP0004` can use Critter Stack's explicit versioned flat-placement compatibility policy. `DOTNETSP0002`, `DOTNETSP0003`, `DOTNETSP0005`, `DOTNETSP0009`, `DOTNETSP0010`, `DOTNETSP0011`, `DOTNETSP0013`, and every mixed failure remain strict and never fall back. Arc uses the same complete project-aware compilation mapping while retaining its existing generated bytes; Arc does not support `--feature-root` and reports `CLI0014` without applying it.

The CLI only loads and analyzes restored source. It never starts the application, invokes application startup, or substitutes runtime observation when strict placement fails.

### Marten and Critter Stack preview

Marten and Critter Stack source generation is a **preview**. It covers representative current and legacy applications, including aggregate event returns, snapshots and reducers, HTTP/message handlers, direct document operations, queries, response wrappers, and outgoing or delayed messages. Generated documents are compiled and checked for stable print/compile/print output before they are returned.

The preview does not claim complete reconstruction of every compiled query, `EventProjection`, multi-stream grouper, tenancy topology, saga, middleware chain, broker option, alias, or upcast. Recognized behavior that cannot be represented is reported with a stable `MARTEN`, `WOLVERINE`, `VOG`, or `GEN` diagnostic instead of being silently invented or omitted. The compatibility report identifies the exact package evidence used for admission and keeps package support separate from semantic review and lowering loss. Review both before using generated output as a migration specification.

Source analysis does not start the target host or connect to PostgreSQL or Chronicle. MSBuild still evaluates the targeted project, so generate only from source you trust.

### Prerequisites

The command loads the project through MSBuild, so the **.NET SDK** must be installed — the same SDK you build the project with.

**Packages must already be restored.** An unrestored project still loads, and yields a compilation in which every framework type reads as missing — which would be reported as a page of unrecognizable artifacts and a document describing nobody's application. It is reported as the one thing that is actually wrong instead:

```text
errors (1):
  error CLI0005: 'Library.Domain' has not been restored, so every type the application
                 references reads as missing — run 'dotnet restore' and generate again
```

The project does **not** have to have been built first. Sources MSBuild generates as part of a build — such as the strongly typed classes a `.resx` file declares with `<Generator>MSBuild:Compile</Generator>` — are produced while the project is read, so the model is derived from exactly what a real build compiles.

### Errors

| Condition | Result |
|---|---|
| `PATH` does not exist | Not-found error. |
| `PATH` is a file that is not a solution or project | Not-found error. |
| No solution or project found in `PATH` or any parent folder | Not-found error. |
| The solution holds no project that is not specs | Validation error (`CLI0001`). |
| A project has not been restored | Validation error (`CLI0005`) naming it; nothing is generated. |
| A project targets several frameworks and `--framework` is omitted | Validation error (`CLI0015`) naming the project and ordered available targets; provider interpretation does not start and no source or provenance is produced. |
| A multi-targeted project does not offer the requested `--framework` | Validation error (`CLI0016`) naming the project and ordered available targets; provider interpretation does not start and no source or provenance is produced. |
| No Arc project of the solution can declare a command or event type | Validation error (`CLI0006`). |
| `--provider` does not name an available bundled provider | Validation error (`CLI0007`) listing the providers in this CLI build. |
| Authored source still has compilation errors after framework-reference repair | Validation error (`CLI0008`); no Screenplay is generated. |
| A solution contains several deployable hosts for a provider that requires one application | Validation error (`CLI0009`) listing the hosts; target one `.csproj` explicitly. |
| No bundled provider recognizes the loaded source | Validation error (`CLI0010`) listing the available providers. |
| Several unrelated providers recognize the loaded source | Validation error (`CLI0011`) listing the candidates; select one with `--provider`. |
| Resolved Marten/Wolverine package provenance, or required application-owned Vogen package provenance, is absent, divergent, or cannot be classified | Validation error (`CLI0012`); compatibility is `Unknown` and source interpretation does not start. |
| A resolved Marten/Wolverine major, or Vogen major newer than 8, is newer than the highest source-reviewed generation | Validation error (`CLI0013`); compatibility is `Unsupported` and source interpretation does not start. |
| `--modules-from-namespace-roots` or `--authoring-only-constructs` is used with Marten or Critter Stack | Warning (`CLI0014`); generation continues without applying the option and lowering fidelity reports loss. |
| A project cannot be read into a compilation | Validation error (`CLI0004`) naming it; the remaining projects are still described. |
| A project or authored document has a rooted, traversing, outside-trusted-workspace, duplicate, or unmapped source path, or a non-git direct closure has no safe non-root common boundary | Validation error (`CLI0017`) naming the project, the reason and, when one path is responsible, that path; no source is interpreted. The path is relative to the directory of the solution or project that was read, or just the file name when the two share nothing but the filesystem root, so the diagnostic never carries a machine's physical root. |
| Strict shared source placement reports a `DOTNETSP####` error other than Critter Stack's explicit sole-`DOTNETSP0004` compatibility case | Validation error preserving the typed subject and outcome; no standard-output document is written and no hidden fallback runs. |
| Generation reports one or more errors, with `--file` | Validation error; the document is written anyway. |
| Generation reports one or more errors, writing to standard output | Validation error; nothing is written. |

An error means the document does not describe the source faithfully — but a document that is 99% right plus honest diagnostics is more useful than nothing at all, so `--file` still writes it. Read the diagnostics before trusting it, and re-run with `screenplay validate` to see what the Screenplay compiler makes of the result.

Standard output is the exception: whatever consumes `cratis screenplay generate > MyApp.play` cannot tell a partial document from a complete one, so nothing is written there. Pass `--file` when you want the partial document.

## `cratis screenplay conform <MODEL_ROOT>`

Checks hand-written application source against your authored model. It runs the same source-adapter pipeline as `generate`, keeps the recovered `.play` source in memory, and asks Screenplay to compare the authored model **before** with the recovered code model **after**. No generated document is written. Nothing needs to be running.

```bash
cratis screenplay conform ./plays --project ./MyApp.slnx
cratis screenplay conform ./MyApp.play --project ./Source/MyApp/MyApp.csproj -o json
```

The .NET SDK and restored application packages are required, as for `generate`. MSBuild evaluates the selected project and may produce intermediate build files; use trusted projects. The catalog therefore declares a `local` effect, not a guarantee that extraction touches no filesystem state.

### Arguments and shared source options

| Argument / option | Description |
|---|---|
| `MODEL_ROOT` | Required root `.play` file with its imports, or folder containing one logical application. No upward model discovery. Empty folders and source errors refuse the check. |
| `--project <PATH>` | Solution (`.slnx`, `.sln`, `.slnf`), project (`.csproj`) or folder to extract. When omitted, discovers upwards from the current directory as `generate` does. |
| `--domain <NAME>` | Recovered domain. Defaults to the authored `domain` name, or `Application` when none is declared, so the application declaration lines up. |

All other [generation source options](#options) are shared unchanged: `--provider`, `--framework`, `--feature-root`, `--module`, `--skip-segments`, `--modules-from-namespace-roots`, and `--authoring-only-constructs`. Use the options that reproduce your authored module/feature/slice placement; a different layout appears as missing and unrealized declarations, not inferred equivalence. There is no `--file` option.

### Classification

Screenplay owns the structural differences; the CLI applies this conformance policy:

| Difference | Category | Blocking |
|---|---|---|
| Declaration present in code, absent from model | `MissingFromModel` | Yes |
| Declaration present in model, absent from code | `NotRealizedInCode` | No |
| Event property or generation contract change | `ShapeMismatch` | Yes |
| Authored member absent from code (`afterHash` absent) | `ShapeMismatch` | No: extraction may not recover it |
| Other member change | `ShapeMismatch` | Yes |
| Specification changes or application metadata member changes | Informational | No |
| Direct dependants | Context only | No |
| Persisted identities | Ignored in address matching | No |

Specification presence changes are also informational. An unmatched addition/removal with the same kind and last name segment carries a `sameNameCounterpart` hint when there is exactly one candidate. The hint never changes blocking.

### Output and exit codes

The default human output shows a findings table, declaration counts, blocking/informational totals and coverage limits. `-o json` and `-o json-compact` report:

| Field | Meaning |
|---|---|
| `verdict` | `clean`, `defects-found`, or `incomplete` |
| `modelRoot`, `project`, `projects` | Resolved model and extraction target; names of projects read |
| `matching` | `Address`: kind plus exact semantic address, without persisted identity continuity |
| `comparedCounts` | Declaration group counts `{model, code}` |
| `blockingCount`, `informationalCount` | Finding counts; informational includes every nonblocking finding |
| `missingFromModel`, `notRealizedInCode`, `shapeMismatches`, `informational` | Findings with `category`, `kind`, `address`, `change`, optional `member`, event `beforeType`/`afterType`, `blocking` and optional `sameNameCounterpart` |
| `dependants` | Direct reference context from Screenplay, not transitive or runtime impact |
| `notCompared` | Stable statements of comparison limits |
| `gaps` | Coverage limits `{section, kind, statement}` |
| `generationDiagnostics` | Number of generation diagnostics |
| `provenance` | Source/provider compatibility provenance, when available, as in `generate` |

Optional null fields are omitted. Generation diagnostics and provenance also go to **standard error** through the existing generation diagnostic writer. Authored source diagnostics are reported there separately. An outright failure reports an error on standard error without claiming a comparison verdict.

This command deliberately uses the organization's check contract rather than the general CLI exit-code table:

| Code | Meaning |
|---|---|
| `0` | No blocking findings and no gap that prevented looking. Model-ahead findings remain listed. This is not behavioral equivalence. |
| `1` | At least one blocking finding, even if coverage also has gaps. |
| `2` | Model/project cannot be resolved, authored source has errors, extraction fails, comparison throws, or (without blocking findings) coverage contains `IncompleteSource`, `ExampleResolution`, `NotComparableAssigned` or `NotComparableIndexed`. |

Expected address-mode limits such as `AddressKeysOnly`, `IdentitiesNotCompared` and `DeclarationLevelOnly` are reported but do not themselves fail the command. Other context limits, including unresolved dependants and unassigned event/specification identities, also remain visible without being defects.

### Limits

Matching is by exact kind and address, not rename inference. A renamed or moved declaration appears as removal plus addition. Without executable binding on both sides, Screenplay falls back to declaration-level comparison and reports that limit. It does not analyze opaque code bodies, open external implementation files, execute specifications or prove behavioral equivalence. Review extraction diagnostics and provenance as well as the verdict; a clean check cannot prove semantics the source provider did not recover.

## `cratis screenplay conform-namespace <MODEL_ROOT>`

`conform` tells you whether the code matches the model; it cannot tell you whether a running system actually did what the model says. `conform-namespace` answers that second question for one Chronicle event store, namespace and event sequence: are the declared event types registered, were undeclared events appended, did each command and reactor append the events the model says it produces, and are the declared reactions subscribed? Screenplay's observation comparison decides the findings; the CLI only reads Chronicle and builds the evidence it compares.

```bash
cratis screenplay conform-namespace ./plays -e Library -n Default
cratis screenplay conform-namespace ./MyApp.play -e Library -n Default --max-events 20000 --fail-on-drift -o json
```

The command reads; it never replays observers, appends events or changes the model.

### Arguments and options

| Argument / option | Description |
|---|---|
| `MODEL_ROOT` | Required root `.play` file with its imports, or folder containing one application. Model errors refuse the command before it connects. |
| `-e, --event-store <NAME>` | Event store to observe. Must be passed or set in the active context; the built-in default is never assumed. Must exist, with the exact name. |
| `-n, --namespace <NAME>` | Namespace to observe, under the same rules as the event store. |
| `--sequence <ID>` | Event sequence to observe (default `event-log`). Any other sequence must exist. |
| `--from <NUMBER>` | First sequence number to examine. Starting after the first event gives partial coverage. |
| `--to <NUMBER>` | Last sequence number to examine. Defaults to the captured tail. |
| `--max-events <COUNT>` | Most events to examine (default `100000`). Reaching it is not an error: the rest of the range is reported as unexamined. |
| `--fail-on-drift` | Fail with `7` on known drift and `8` when the comparison is inconclusive. |

A name that differs from an existing event store, namespace or sequence only in case is reported with the existing name; it is never chosen for you.

### What is read

1. The tail of the event sequence, **once, first**. The scan never reads past it, so events appended while it runs cannot shift the coverage.
2. The whole event type registry of the event store, and the registered observers of the namespace.
3. Event **metadata** from the first examined number to the last, 500 sequence numbers per request. Payloads are never requested.

Each appended event is attributed through its causation chain to the innermost command (the `commandType` an Arc command records) or reactor (the `ReactorId` the Chronicle client records). An event whose chain names neither counts as unattributed. Only the producer's name is read; command values and the rest of the causation never leave the reader. Event source ids, subjects and content are not read into the comparison and never printed. Chronicle's metadata API still sends event source ids and subjects over the wire with each page; the command drops them when mapping the response and never outputs them.

Observers become consumers: reactors are compared as reactions, while projections and reducers are reported as Unknown by Screenplay. A model reaction is mapped to a reactor only when its name is unique in the model and exactly one application reactor's identifier, or its last segment, equals it; anything else stays unmapped and its findings are Unknown rather than guessed.

### Output and exit codes

The human output prints the verdict (`drift`, `no-drift` or `inconclusive`), the scope, the examined range against the captured tail, the event totals, every finding as `[Outcome] statement` and every limit Screenplay states as not established. `-o json` and `-o json-compact` write Screenplay's observation report JSON unchanged, with the same statements; `hasDrift` is omitted, never written as `false`, when drift can be neither established nor ruled out. `-o quiet` prints only the verdict.

| Code | Meaning |
|---|---|
| `0` | The comparison ran. Drift is reported but does not fail the command unless `--fail-on-drift` is passed. |
| `7` (`drift_detected`) | With `--fail-on-drift`: Screenplay found known drift. |
| `8` (`inconclusive`) | With `--fail-on-drift`: no known drift, but the scan was partial or a finding is Unknown. Also returned, with or without `--fail-on-drift`, when the scan is interrupted (Ctrl+C); nothing is compared. |
| `5` (`validation_error`) | Model errors, no selected event store or namespace, invalid bounds, or a missing event store, namespace or sequence. |
| `2` (`connection_error`) | The server could not be reached. |
| `3` (`server_error`) | Chronicle refused or failed a read. No snapshot is built and nothing is compared. |
| `4` (`authentication_error`) | Chronicle refused a read for lack of authorization. |

A failed read always fails the command; it is never presented as an empty store.

### Limits

Chronicle does not expose the generation an event was appended with in its metadata, so each appended event counts against the highest registered generation of its type, which is the generation Chronicle presents stored events as; an event of an unregistered type counts as generation 1. One sequence is compared per run. The comparison does not check payloads, execute specifications or prove behavioral correctness; read the `notEstablished` statements in every report.

## `cratis screenplay baseline`

Protect released event contracts from accidental edits outside MCP. `check` compares the current model with the admitted release baseline without reconciling or writing identities. `admit` monotonically records released event revisions and atomically writes the baseline using Screenplay's recovery journal.

Both commands require persisted `.screenplay/identities.json`, created by an applied MCP proposal. `ROOT` is the model directory and defaults to the current directory, honoring `mcpServers.screenplay.root` in `.cratis/ai.json` through the existing MCP root resolver. Pass an explicit root to select a different model.

[`cratis render` reports release-baseline warnings](#release-baseline-warnings) without refusing publication or changing its exit code. Keep `baseline check` as the read-only gate in CI:

```bash
cratis screenplay baseline check ./Model
```

In your release job, after choosing the release tag, admit the shipped revisions and retain the updated `.screenplay/release-baseline.json` alongside the model and its identities:

```bash
cratis screenplay baseline admit ./Model --release "$TAG"
```

`--release <LABEL>` is required. Add `--from-esm <FILE>` to admit a canonical shipped ESM instead of the current model. Admission does not authorize changing an already admitted revision. An absent baseline can be checked, but absent identities, unreadable metadata and pending recovery refuse the operation.

Plain text is the default. Violations print as `Kind: Event revision N`, followed by `at Path` when available. Failures go to standard error as `FailureKind: Reason`. Successful admission prints `Admitted N event revision(s) in release baseline REVISION.`

Use `-o json` or `-o json-compact` for a standard-output object containing `violations`, `failureKind`, `reason`, `admittedEntryCount`, `baselineRevision` and `exitCode`; null fields are omitted.

These commands use **check-specific exit codes**, not the general CLI codes:

| Exit code | Meaning |
|---|---|
| `0` | Clean check or successful admission. |
| `1` | Released event revision violations. |
| `2` | Unreadable, refused, or could not run. |
| `-1` (`255` on POSIX) | CLI usage error, such as unknown options or a missing `--release`; unexpected failures also use this code. |

## `cratis screenplay validate [PATH]`

Compiles Screenplay documents and reports everything the compiler found. It does not care what wrote them — `screenplay generate`, [`cratis prologue`](prologue.md), or a person designing a system before any code exists.

`PATH` is a Screenplay (`.play`) file, or a folder. A file is the root of an application: it is compiled together with every document its imports bring in, diagnostics name the imported document they belong to, and the reported file count includes imported documents. A folder is compiled as one application: declarations are merged before resolution, so a concept, event, or policy declared in one file resolves when another file references it. Keep unrelated applications in separate root folders, or target each application file individually. `PATH` defaults to the current directory.

```bash
cratis screenplay validate                 # one application from every .play file beneath the current folder
cratis screenplay validate ./MyApp.play    # a root document and its imports
cratis screenplay validate ./plays         # one application from every .play file beneath a folder
cratis screenplay validate --warnings-as-errors ./plays # fail on compiler warnings
cratis screenplay validate --executable ./plays # also check that the model binds
cratis screenplay validate --executable --target cratis --name MyApp ./plays # also check bundled target admission, without writing
cratis screenplay validate ./plays --check all # opt into structural completeness warnings
cratis screenplay validate ./plays --check navigation --check field-origins --executable
```

`--warnaserror` is an alias of `--warnings-as-errors`; both fail on compiler and completeness warnings with exit `5`. Unknown options fail with exit `5` rather than being ignored.

### Declared validation gate

Commit `screenplay.json` at your model root to share a validation gate with the standalone `screenplay` tool:

```json
{
  "validation": {
    "checks": ["field-origins", "query-keys", "navigation"],
    "warningsAsErrors": true,
    "scope": "Billing.Invoices",
    "openQuestions": "fail"
  }
}
```

Every member is optional. Each value resolves independently: **explicit command-line flag → file → default**. Defaults are no completeness checks, warnings do not fail, no scope, and no notes evaluation. Any `--check` selections replace the file's list rather than extend it; repeated or comma-separated CLI selections still combine. Omitting `--warnaserror` retains a file's `warningsAsErrors: true`. File check items must each be one name, diagnostic-code alias or `all`, not comma-separated lists; an empty list selects no checks. `all` expands to the running compiler's check catalog, so upgrading can strengthen the gate.

Root search starts at the target folder or a file's parent and selects the nearest folder containing `screenplay.json` or `.screenplay/`. It includes but never crosses the first `.git` boundary; without a match, it uses the starting folder. Nested models keep their own gates. Imports do not affect root search, and finding a gate does not widen the compiled source set.

| Option | Effect |
|---|---|
| `--no-gate-file` | Ignore the file, including an invalid or unreadable file; use explicit flags and defaults. |
| `--print-gate` | Print the effective gate and each value's source, then exit `0` without compiling. Text prints labeled values; JSON output modes print the gate object; quiet prints nothing. A refused file still exits `1` unless bypassed. |

```bash
cratis screenplay validate ./plays --print-gate
cratis screenplay validate ./plays --print-gate -o json-compact
cratis screenplay validate ./plays --no-gate-file --check navigation
```

Malformed JSON, duplicate members, unknown keys/checks and wrong types refuse validation with exit `1` and a `path:line:column` reason. A directory, symlink/reparse point or unreadable gate file also refuses; none is treated as absence or repaired. Use `--no-gate-file` to bypass a refused file deliberately. See [Screenplay's authored gate contract and schema](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#declare-the-validation-gate-once) for the shared file format.

JSON validation summaries gain `gate`, with `modelRoot`, `gateFilePath`, and `checks`, `warningsAsErrors`, `scope`, `openQuestions`, each holding `{ "value": ..., "source": "commandLine|file|default" }`. Checks are expanded canonical names; unselected scope and questions have null values. The existing top-level `checks` remains. When a file declares a gate, compiler failures also produce a summary with the effective gate; diagnostics remain on standard error.

### Model questions

Add `--open-questions report|fail` or declare `validation.openQuestions` to read `.screenplay/notes.json` after compiler diagnostics. The option cannot be repeated; notes are ignored when neither selects a mode.

```bash
cratis screenplay validate ./plays --open-questions report
cratis screenplay validate ./plays --scope Billing.Invoices.SendInvoice --open-questions fail
```

`report` lists open questions without failing because they remain. `fail` exits `5` when an open question remains in the selected set; unreadable notes or identity state exit `1`. Compiler errors still exit `5`. Question scope includes descendants and ancestor notes. Without persisted identity state, Screenplay reports all questions unscoped as orphaned rather than hiding them. Tasks and ordinary notes do not fail this gate, and questions do not participate in `--warnings-as-errors`.

With a declared gate or `--no-gate-file`, notes are read from the resolved model root. Otherwise, a file target reads notes from its parent directory and a folder from that folder, preserving the flag-only behavior. `--print-gate` never reads notes or compiles. JSON summaries include `openQuestions` with `findings`, identity/notes statuses, effective scope and any failure details.

### Scoped validation

Keep the whole application as `PATH` and select the part to report with `--scope <Module>[.<Feature>[.<Slice>]]`:

```bash
cratis screenplay validate ./plays --scope Billing.Invoices.SendInvoice
cratis screenplay validate ./plays --scope Billing --check all --warnings-as-errors
```

Addresses are case-sensitive and include full dotted paths for nested features. The selected scope includes descendants and declarations that directly reference them, not transitive dependents or whole neighboring slices. Compilation still resolves the whole application. Unknown or ambiguous scopes are usage errors (exit `1`, the CLI's invalid-input/not-found code), never empty successful checks.

The exit code and `--warnings-as-errors` cover only reported diagnostics: a clean scope exits `0` even when the rest of the application has errors. Console output labels counts **In scope** and always shows a separate **Whole application** error/warning summary. JSON always includes the scoped `valid`, `errors`, `warnings` and `scope`, plus `wholeApplication` with its own `valid`, `errors` and `warnings`. It also reports declaration and direct-dependent counts, affected scopes, unresolved event consumers, and dependency coverage limits. A clean scope is not proof that the whole application is valid.

`--scope` combines with `--check`, but completeness runs only when the whole application's source is error-free. It cannot combine with `--executable`: executable binding is a whole-application verdict, so check it separately. A scope declared in `screenplay.json` with `--executable` exits `1`, naming the file and suggesting `--no-gate-file`; a CLI-given `--scope` with `--executable` retains its CLI usage error. Scope impact uses explicit source references, not inline code, property paths or expression identifiers; unresolved references are included conservatively where they match selected declarations. See [Screenplay's scope contract](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#check-one-part-of-the-application) for coverage limits.

### Completeness checks

`--check <SELECTION>` opts into structural completeness checks. Repeat the option or use comma-separated selections; all selections are combined without duplicates. Select a check by name, by any of its diagnostic codes, or use `all` to select every check:

| Name | Diagnostic codes |
|---|---|
| `data-bindings` | `PLAY0530`, `PLAY0531` |
| `input-surfaces` | `PLAY0532`, `PLAY0533` |
| `field-origins` | `PLAY0534` |
| `query-keys` | `PLAY0535` |
| `event-consumers` | `PLAY0536` |
| `navigation` | `PLAY0537` |
| `personas` | `PLAY0574`, `PLAY0575`, `PLAY0576` |
| `purposes` | `PLAY0602`, `PLAY0603`, `PLAY0604`, `PLAY0605`, `PLAY0606` |
| `privilege` | `PLAY0652` |
| `enum-pinning` | `PLAY0671` |

`all` selects every check in the table. Names and diagnostic codes are case-sensitive. An unrecognized selection is a validation error (exit `5`) that names the selection and lists the accepted names and codes. Checks run only when source compilation succeeds. If the model has source errors, the summary reports `completeness checks skipped: the model has N error(s)` rather than completeness findings. `--executable` can be used together with `--check`; binding remains a separate verdict.

Findings are warnings and fail validation only with `--warnings-as-errors`. These checks inspect structure; a finding is a prompt to look, and neither findings nor their absence prove runtime completeness.

The JSON summary includes `valid` (whether validation passed), `errors` and `warnings` counts, `checks` (the selected names) and `completenessStatus` (`ran`, `skipped`, or `not requested`). When checks are skipped, `completenessNote` explains why. Requested checks also produce a summary when validation fails; diagnostics remain on standard error.

### Compiler diagnostics

Diagnostics go to **standard error**, grouped by severity with errors first, in the same shape `generate` uses. Each line carries the compiler's `PLAY` code, then the file and the position within it:

```text
errors (1):
  error PLAY0027: [MyApp.play(5,5)] Invalid slice declaration 'slice Reserving' - expected 'slice <Type> <Name>'

warnings (1):
  warning PLAY0166: [MyApp.play(787,11)] Unknown event 'InvitationToJoinAdaAccepted' - declare it with 'event InvitationToJoinAdaAccepted'
```

With `-o json` or `-o json-compact` the same diagnostics are written to standard error as a JSON object instead.

**Warnings and information do not fail the command by default. An error does.** Pass `--warnings-as-errors` when a CI gate must also reject compiler warnings.

### Valid, executable and renderable

These are three separate verdicts; target admission is opt-in during validation:

| Verdict | Means | Checked by |
|---|---|---|
| Valid | The documents parse, merge into one application and pass the source checks. | `cratis screenplay validate` |
| Executable | The application also binds into an executable semantic model (ESM). Constructs no ESM version admits yet, such as event sources and streams, report `PLAY0268` here. | `cratis screenplay validate --executable` |
| Renderable | The bundled renderer target also admits every construct in that model and plans its artifacts. | `cratis render`, or `cratis screenplay validate --executable --target cratis` (in memory) |

`--executable` binds the documents exactly as `cratis render` does, including implementation attachments, but, without `--target`, never plans, renders or writes anything and needs no container runtime. Like rendering, it binds only documents beneath the model root, which is the folder, or a single file's parent directory; a document a root file imports from above that folder reports `CLI-VALIDATE-001`, so validate the folder that holds every document instead. Documents the binder cannot represent, such as a file path it does not admit, report `CLI-VALIDATE-002`. A plain pass says only that the source is valid: the summary reports `checked: source` and the text output says so. With `--executable` the JSON summary adds `executable: true`, and a model that does not bind fails with a validation error and its binding diagnostics.

Use `--executable --target cratis` to additionally run the bundled Stage planner in memory. Without `--target`, validation remains target-neutral, including its existing exit codes: successful binding does not imply that Stage admits the model. `--no-render-admission` skips the target check even when `--target cratis` is present. No validation mode publishes artifacts or changes files. `--name` sets the application name for admission and is ignored without `--target`; otherwise the model-root workspace name or source folder name is used. A persisted workspace retains its application name, so an explicit conflicting name is refused.

The opt-in summary reports `binding.executable` separately from `targetAdmission.status` (`passed`, `failed`, or `skipped`), with target diagnostics, refusals and implementation gaps. Binding errors skip admission. Target errors fail with exit `5`; target warnings follow `--warnings-as-errors`, just like source diagnostics.

Stage diagnostics in validation, `render` and `render --check` resolve semantic identities to the compiled `.play` source span as `file(line,column)` when a source-map entry exists. JSON diagnostics retain the original semantic identity in `subject`; unmapped identities remain available as the location. These are target-admission findings, not source compilation errors.

### Validation outcomes

| Condition | Exit code | Result |
|---|---|---|
| Source is valid, and binding succeeds if `--executable` is set | `0` | Success. Warnings do not fail unless treated as errors. |
| `--print-gate` resolves a gate | `0` | Prints the gate without compiling, even if the model source is broken. |
| `screenplay.json` is invalid or unreadable | `1` | Located refusal; fix the file or use `--no-gate-file`. |
| A file-declared scope is combined with `--executable` | `1` | Invalid input; check binding separately or use `--no-gate-file`. |
| `PATH` does not exist | `1` | Not-found error. |
| `PATH` is a file that is not a `.play` file | `1` | Not-found error. |
| No `.play` file found in the folder | `1` | Not-found error — validating nothing is never the answer you wanted. |
| A scope is unknown, ambiguous, or cannot be read | `1` | Invalid-input/not-found error. |
| A folder declares more than one domain | `5` | Validation error — a folder describes one application, which can have at most one domain. |
| Compilation reports one or more errors | `5` | Validation error. |
| Compilation or completeness checks report warnings and the effective gate treats warnings as errors | `5` | Validation error. |
| A `--check` selection is unrecognized | `5` | Validation error naming the selection and listing accepted names and codes. |
| An option is unknown | `5` | Validation error naming the option and pointing to help. |
| `--open-questions fail` reports open questions | `5` | Validation failed; findings remain visible. |
| `--open-questions` is invalid or repeated | `5` | Validation error requiring one `report` or `fail` value. |
| Requested model notes or identity state cannot be read | `1` | Invalid input; compiler errors still take precedence with exit `5`. |
| `--executable` is set and the model does not bind | `5` | Validation error. |
| CLI usage is invalid, such as a missing option value or combining `--scope` with `--executable` | `-1` (`255` on POSIX) | CLI usage error. Unexpected failures also use this code. |

These are the Cratis CLI's exit codes, not the standalone `screenplay` tool's codes. With `--scope`, the validation result covers only the reported diagnostics; whole-application counts remain separate.

## Where a Screenplay comes from

Generating from source is one of three ways to arrive at a `.play` file, and they meet in the same place:

- **From source** — `screenplay generate`, for an Arc, Marten, or Critter Stack application. Needs the .NET SDK and a restored checkout; needs nothing running.
- **From a running system** — [`cratis prologue`](prologue.md) captures what a system does and interprets it into a Screenplay, for systems built without Cratis.
- **By hand** — write the `.play` file as the design, before any code exists.

Whichever route you take, `screenplay validate` compiles the result and [`cratis run`](run.md) boots it in a local Stage sandbox.
