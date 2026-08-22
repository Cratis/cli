# Screenplay

`cratis screenplay` works with Cratis Screenplay (`.play`) documents. It generates one from Arc, Marten, or Marten + Wolverine application source — so the event model your team reads is derived from the code that actually runs rather than maintained alongside it — and it compiles the documents you already have.

```bash
cratis screenplay generate [PATH]
cratis screenplay validate [PATH]
```

**Nothing needs to be running.** This is what separates `cratis screenplay` from [`cratis arc`](../arc/index.md): every `arc` command talks to a *running* application over HTTP, while `screenplay` only ever reads files. The result is reproducible from a checkout — commit it, diff it, and run it in CI, on a machine where the application was never started.

Fetching a `.play` document from a running application over an introspection endpoint is a separate, complementary route: it trades the SDK requirement for the requirement that the application be running. Source generation remains reproducible from a restored checkout and does not execute application startup or connect to Chronicle/PostgreSQL.

## `cratis screenplay generate [PATH]`

Reads a solution or project, discovers the source providers bundled with this CLI, derives the event model from the framework artifacts and conventions they recognize, and writes a Screenplay document. The bundled providers currently cover Arc, Marten, and Marten + Wolverine (Critter Stack). The CLI owns discovery and orchestration; each provider package owns its framework semantics.

Auto detection chooses the most specific matching provider. For example, Critter Stack supersedes its Marten foundation when both Marten and Wolverine are present. If unrelated providers match, the CLI reports the candidates rather than guessing. Use `--provider` as an explicit override for mixed applications and reproducible CI.

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
| `--domain <NAME>` | Name of the domain the generated document belongs to. Defaults to the assembly or root namespace of the project, and to the solution name when several projects are read. |
| `--module <NAME>` | Name of the module every discovered feature is placed within. Defaults to the domain. |
| `--skip-segments <COUNT>` | Number of leading namespace segments to skip when inferring features and slices. |
| `--modules-from-namespace-roots` | With the Arc provider, name each feature's module after the outermost namespace segment. Marten/Critter Stack currently report `CLI0014` and leave this option unapplied. |

The output file uses `--file` rather than `-o`, because `-o/--output` is the global output *format* flag — see [Global Options](global-options.md).

```bash
cratis screenplay generate
cratis screenplay generate ./MyApp.slnx --file MyApp.play
cratis screenplay generate ./Source/MyApp/MyApp.csproj
cratis screenplay generate ./Banking.csproj --provider marten --file Banking.play
cratis screenplay generate ./Helpdesk.csproj --provider critter-stack --file Helpdesk.play
cratis screenplay generate --domain Library --module Lending --file Library.play
```

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

A project that targets several frameworks is read once. The workspace opens it once per target framework and names the results `MyApp(net10.0)`, `MyApp(net9.0)`; the CLI selects one deterministically and the provenance report names the selected target. Package admission, capabilities, and the support tier apply only to that reported target framework — they make no claim about the project's other targets.

Pass a `.csproj` instead of the solution to describe a single project — pointing at a project is the instruction to read it, so it is read whatever it can see.

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

With `-o json` or `-o json-compact`, standard error is one JSON object containing both source provenance and diagnostics.

**Warnings and information do not fail the command** — the document is still written. **An error does**: nothing is written and the command exits with a validation error, because a document that does not describe the source faithfully is worse than no document.

### Source and compatibility provenance

Every successful provider selection reports provenance on standard error, separately from the `.play` document on standard output. The report names:

- the selected provider and bundled provider package version;
- every selected project and target framework;
- resolved Marten/Wolverine NuGet package IDs and versions from that target's `project.assets.json`;
- referenced framework assembly identities and versions as corroboration;
- exact metadata capability fingerprints found by Roslyn.

For Marten and Critter Stack, it also reports four independent compatibility dimensions:

- **Support tier** — `Canonical`, `SourceReviewed`, `RecognizedWithLoss`, `Unknown`, or `Unsupported` package/API evidence.
- **Recognition status** — whether the provider recognized the framework generation.
- **Semantic conformance** — whether static interpretation completed and still requires human review, found contradictory evidence, or was not evaluated.
- **Lowering fidelity** — whether no loss was reported, loss was reported, lowering failed, or lowering was not evaluated.

These values deliberately do not imply one another. A canonical package set can still use behavior outside its fixture assertions, require human review, and report lowering loss. An assembly version corroborates a NuGet version but never replaces it.

```text
source compatibility:
  provider: critter-stack 0.13.0
  project: Helpdesk.Api (net9.0)
    packages: Marten 9.23.0, WolverineFx 6.29.1, WolverineFx.Marten 6.29.1
    assemblies: Marten 9.23.0.0, Wolverine 6.29.1.0
    capabilities: marten.event-projection, wolverine.handler-attribute
  support tier: Canonical
  recognition: Recognized
  semantic conformance: RequiresHumanReview
  lowering fidelity: LossReported
```

`Canonical` means the bundled provider version passes the exact pinned package set and its fixture assertions — not that every API in that package combination is implemented. A package set that is canonical for a newer adapter remains `SourceReviewed` when the CLI bundles an older provider. `SourceReviewed` otherwise means the major-generation source and metadata were reviewed, but the exact provider/package combination is not canonical. `RecognizedWithLoss` means the API is identified but its source semantics cannot be interpreted exactly. `Unknown` and `Unsupported` fail closed before source interpretation. A newer Marten or Wolverine major remains unsupported until source review and canonical evidence exist.

Arc reports provider, target-framework, package, assembly, and capability provenance but continues to use its existing adapter compatibility contract rather than the Critter Stack support-tier matrix.

### Marten and Critter Stack preview

Marten and Critter Stack source generation is a **preview**. It covers representative current and legacy applications, including aggregate event returns, snapshots and reducers, HTTP/message handlers, direct document operations, queries, response wrappers, and outgoing or delayed messages. Generated documents are compiled and checked for stable print/compile/print output before they are returned.

The preview does not claim complete reconstruction of every compiled query, `EventProjection`, multi-stream grouper, tenancy topology, saga, middleware chain, broker option, alias, or upcast. Recognized behavior that cannot be represented is reported with a stable `MARTEN`, `WOLVERINE`, or `GEN` diagnostic instead of being silently invented or omitted. The compatibility report identifies the exact package evidence used for admission and keeps package support separate from semantic review and lowering loss. Review both before using generated output as a migration specification.

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
| No Arc project of the solution can declare a command or event type | Validation error (`CLI0006`). |
| `--provider` does not name an available bundled provider | Validation error (`CLI0007`) listing the providers in this CLI build. |
| Authored source still has compilation errors after framework-reference repair | Validation error (`CLI0008`); no Screenplay is generated. |
| A solution contains several deployable hosts for a provider that requires one application | Validation error (`CLI0009`) listing the hosts; target one `.csproj` explicitly. |
| No bundled provider recognizes the loaded source | Validation error (`CLI0010`) listing the available providers. |
| Several unrelated providers recognize the loaded source | Validation error (`CLI0011`) listing the candidates; select one with `--provider`. |
| Resolved Marten/Wolverine package provenance is absent, divergent, or cannot be classified | Validation error (`CLI0012`); compatibility is `Unknown` and source interpretation does not start. |
| A resolved Marten/Wolverine major is newer than the highest source-reviewed generation | Validation error (`CLI0013`); compatibility is `Unsupported` and source interpretation does not start. |
| `--modules-from-namespace-roots` is used with Marten or Critter Stack | Warning (`CLI0014`); generation continues without applying the option and lowering fidelity reports loss. |
| A project cannot be read into a compilation | Validation error (`CLI0004`) naming it; the remaining projects are still described. |
| Generation reports one or more errors, with `--file` | Validation error; the document is written anyway. |
| Generation reports one or more errors, writing to standard output | Validation error; nothing is written. |

An error means the document does not describe the source faithfully — but a document that is 99% right plus honest diagnostics is more useful than nothing at all, so `--file` still writes it. Read the diagnostics before trusting it, and re-run with `screenplay validate` to see what the Screenplay compiler makes of the result.

Standard output is the exception: whatever consumes `cratis screenplay generate > MyApp.play` cannot tell a partial document from a complete one, so nothing is written there. Pass `--file` when you want the partial document.

## `cratis screenplay validate [PATH]`

Compiles Screenplay documents and reports everything the compiler found. It does not care what wrote them — `screenplay generate`, [`cratis prologue`](prologue.md), or a person designing a system before any code exists.

`PATH` is a Screenplay (`.play`) file, or a folder — in which case every `.play` file beneath it is compiled. It defaults to the current directory.

```bash
cratis screenplay validate                 # every .play file beneath the current folder
cratis screenplay validate ./MyApp.play    # one document
cratis screenplay validate ./plays         # every .play file beneath a folder
```

### Compiler diagnostics

Diagnostics go to **standard error**, grouped by severity with errors first, in the same shape `generate` uses. Each line carries the compiler's `PLAY` code, then the file and the position within it:

```text
errors (1):
  error PLAY0027: [MyApp.play(5,5)] Invalid slice declaration 'slice Reserving' - expected 'slice <Type> <Name>'

warnings (1):
  warning PLAY0166: [MyApp.play(787,11)] Unknown event 'InvitationToJoinAdaAccepted' - declare it with 'event InvitationToJoinAdaAccepted'
```

With `-o json` or `-o json-compact` the same diagnostics are written to standard error as a JSON object instead.

**Warnings and information do not fail the command. An error does** — which is what makes this usable as a CI gate on a committed `.play` file.

### Validation outcomes

| Condition | Result |
|---|---|
| `PATH` does not exist | Not-found error. |
| `PATH` is a file that is not a `.play` file | Not-found error. |
| No `.play` file found in the folder | Not-found error — validating nothing is never the answer you wanted. |
| Compilation reports one or more errors | Validation error. |

## Where a Screenplay comes from

Generating from source is one of three ways to arrive at a `.play` file, and they meet in the same place:

- **From source** — `screenplay generate`, for an Arc, Marten, or Critter Stack application. Needs the .NET SDK and a restored checkout; needs nothing running.
- **From a running system** — [`cratis prologue`](prologue.md) captures what a system does and interprets it into a Screenplay, for systems built without Cratis.
- **By hand** — write the `.play` file as the design, before any code exists.

Whichever route you take, `screenplay validate` compiles the result and [`cratis run`](run.md) boots it in a local Stage sandbox.
