# Screenplay

`cratis screenplay` works with Cratis Screenplay (`.play`) documents. It generates one from the source code of a Cratis Arc application — so the event model your team reads is derived from the code that actually runs rather than maintained alongside it — and it compiles the documents you already have.

```bash
cratis screenplay generate [PATH]
cratis screenplay validate [PATH]
```

**Nothing needs to be running.** This is what separates `cratis screenplay` from [`cratis arc`](../arc/index.md): every `arc` command talks to a *running* application over HTTP, while `screenplay` only ever reads files. The result is reproducible from a checkout — commit it, diff it, and run it in CI, on a machine where the application was never started.

Fetching a `.play` document from a running Arc application over its introspection endpoint is a separate, complementary route: it trades the SDK requirement for the requirement that the application be running. That route does not exist yet — neither the Arc endpoint nor a CLI command for it — so generating from source is today the only way to derive a Screenplay from a Cratis Arc application.

## `cratis screenplay generate [PATH]`

Reads a solution or project, derives the event model from the Arc artifacts it finds — commands, events, read models, projections, reactors, constraints, and the concepts they are built from — and writes a Screenplay document.

By default the document goes to standard output, so it composes with the shell:

```bash
cratis screenplay generate > MyApp.play
```

Pass `--file` to write it directly instead. The output is written as raw UTF-8, byte for byte, ending in exactly one newline — regenerating an unchanged model produces an identical file.

### Arguments

| Argument | Description |
|---|---|
| `PATH` | Solution (`.slnx`, `.sln`), project (`.csproj`), or folder to read. Defaults to the current directory. |

### Options

| Option | Description |
|---|---|
| `--file <FILE>` | File to write the generated Screenplay to. Writes to standard output when not given. |
| `--domain <NAME>` | Name of the domain the generated document belongs to. Defaults to the assembly or root namespace of the project, and to the solution name when several projects are read. |
| `--module <NAME>` | Name of the module every discovered feature is placed within. Defaults to the domain. |
| `--skip-segments <COUNT>` | Number of leading namespace segments to skip when inferring features and slices. |

The output file uses `--file` rather than `-o`, because `-o/--output` is the global output *format* flag — see [Global Options](global-options.md).

```bash
cratis screenplay generate
cratis screenplay generate ./MyApp.slnx --file MyApp.play
cratis screenplay generate ./Source/MyApp/MyApp.csproj
cratis screenplay generate --domain Library --module Lending --file Library.play
```

### Finding the solution or project

When `PATH` is a solution or project file, that file is read. When it is a folder — or is omitted entirely — the CLI looks in that folder and then in each parent folder in turn, stopping at the first one that holds a match. Within a folder it prefers `.slnx`, then `.sln`, then `.csproj`. Two candidates of the same kind in one folder is reported rather than guessed at.

A Screenplay describes one application, and an application is regularly split across several projects — an executable alongside the libraries holding its slices. Every project of a solution therefore takes part in the same document, except the ones whose name ends in `.Specs`, `.Specifications`, `.Tests`, `.Test`, or `.IntegrationTests`.

The projects that were read are named in the result, so you can see what the document covers:

```text
Projects:    Library.Api, Library.Domain, Library.ReadModels
```

Pass a `.csproj` instead of the solution to describe a single project.

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

With `-o json` or `-o json-compact` the same diagnostics are written to standard error as a JSON object instead.

**Warnings and information do not fail the command** — the document is still written. **An error does**: nothing is written and the command exits with a validation error, because a document that does not describe the source faithfully is worse than no document.

### Prerequisites

The command loads the project through MSBuild, so the **.NET SDK** must be installed — the same SDK you build the project with. Packages must be restorable; a project that cannot be restored cannot be read.

The project does **not** have to have been built first. Sources MSBuild generates as part of a build — such as the strongly typed classes a `.resx` file declares with `<Generator>MSBuild:Compile</Generator>` — are produced while the project is read, so the model is derived from exactly what a real build compiles.

### Errors

| Condition | Result |
|---|---|
| `PATH` does not exist | Not-found error. |
| `PATH` is a file that is not a solution or project | Not-found error. |
| No solution or project found in `PATH` or any parent folder | Not-found error. |
| The solution holds no project that is not specs | Validation error. |
| A project cannot be read into a compilation | Validation error naming it; the remaining projects are still described. |
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

Diagnostics go to **standard error**, grouped by severity with errors first, in the same shape `generate` uses. The compiler does not assign codes, so each line carries the file and the position within it instead:

```text
errors (1):
  error: [MyApp.play(5,5)] Invalid slice declaration 'slice Reserving' - expected 'slice <Type> <Name>'

warnings (1):
  warning: [MyApp.play(787,11)] Unknown event 'InvitationToJoinAdaAccepted' - declare it with 'event InvitationToJoinAdaAccepted'
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

- **From source** — `screenplay generate`, for an application that already exists in Cratis Arc. Needs the .NET SDK and a checkout; needs nothing running.
- **From a running system** — [`cratis prologue`](prologue.md) captures what a system does and interprets it into a Screenplay, for systems built without Cratis.
- **By hand** — write the `.play` file as the design, before any code exists.

Whichever route you take, `screenplay validate` compiles the result and [`cratis run`](run.md) boots it in a local Stage sandbox.
