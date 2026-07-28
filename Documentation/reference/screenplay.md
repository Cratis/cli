# Screenplay

`cratis screenplay` works with Cratis Screenplay (`.play`) documents. Today it generates one from the source code of a Cratis Arc application, so the event model your team reads is derived from the code that actually runs rather than maintained alongside it.

```bash
cratis screenplay generate [PATH]
```

Unlike `cratis arc`, nothing needs to be running. The command reads a solution or project with Roslyn, so the output is reproducible from a checkout — commit it, diff it, and regenerate it in CI.

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
| `--domain <NAME>` | Name of the domain the generated document belongs to. Defaults to the assembly or root namespace of the project. |
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

A Screenplay describes one application, so a solution has to narrow down to a single project:

1. Projects whose name ends in `.Specs`, `.Specifications`, `.Tests`, `.Test`, or `.IntegrationTests` are dropped.
2. If any project that remains produces an executable, the libraries are dropped — an Arc application is the executable.
3. Exactly one project must remain. Anything else is reported, listing the candidates, so you can pass the project you meant.

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
| The solution holds more than one candidate project | Validation error listing the candidates. |
| Generation reports one or more errors | Validation error; no document is written. |

## Where a Screenplay comes from

Generating from source is one of three ways to arrive at a `.play` file, and they meet in the same place:

- **From source** — this command, for an application that already exists in Cratis Arc.
- **From a running system** — [`cratis prologue`](prologue.md) captures what a system does and interprets it into a Screenplay, for systems built without Cratis.
- **By hand** — write the `.play` file as the design, before any code exists.

Whichever route you take, [`cratis run`](run.md) boots the result in a local Stage sandbox.
