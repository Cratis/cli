---
title: Creating projects with cratis new
description: Scaffold a complete, buildable Cratis application in C#, Kotlin, or Java with one command.
---

You want to start a new Cratis application. `cratis new` gets you there in one command: a
complete, buildable project — an Arc backend, a React frontend, and the Cratis AI configuration
wired in — in the language you choose.

```bash
cratis new
cratis new list
cratis new --language csharp -n MyApp -o MyApp
```

The first command is the creation wizard: which project (defaulting to `cratis`, the full-stack
web application), then which language, then which database — each question offering only what
the chosen project supports, and single-choice questions skipped because their choice is already
made. The second lists what's available. The third scaffolds the Cratis web application
directly — the wizard's answers, passed as flags.

## Languages

The same `cratis` web application scaffolds across three language runtimes — pick the one your
team ships in:

| Language | Flag |
| --- | --- |
| C# | `--language csharp` (`c#` is also accepted) |
| Kotlin | `--language kotlin` |
| Java | `--language java` |

Every language gets the same shape: an Arc backend, a React frontend built with Vite, and the
Cratis AI configuration synchronized before you write a line of code. `--language` is required
when scaffolding directly. Other project shapes — a Chronicle-only console app, a web app with
.NET Aspire orchestration — are listed in the [project catalogue](templates.md).

## How it reads

- `cratis new` — the creation wizard: which project (default `cratis`), which language, which
  database. Non-interactive terminals get guidance instead of a hang; `--no-prompts` forces the
  non-interactive behavior everywhere.
- `cratis new list` — list what's available, with each entry's supported languages and
  databases. A project offered in more than one language (C#, Kotlin, and Java `cratis`)
  collapses into a single entry; the language question (or `--language`) picks the runtime.
- `--language` — **required when scaffolding.** The language selects the project used when none
  is named: `csharp` (C#; `c#` is accepted) scaffolds the `cratis` project today, and `kotlin`
  and `java` light up their own runtimes as those ship. Matched case-insensitively; a language
  that isn't published yet is a named error, never a silent fallback to C#.
- `cratis new <project> --language <name>` — scaffold a specific project for that language, e.g.
  `cratis new cratis-aspire --language csharp`.
- `-n/--name` — the project name. Defaults to the project's own default, falling back to the
  output folder name.
- `-o/--output` — the output directory. This is the one place `cratis new` deliberately diverges
  from the rest of the CLI: everywhere else `-o` selects the output *format*, but here it is the
  output *directory*. Use `--format json` for machine-readable output.
- Project-specific options are passed as `--<Option> <value>`, e.g. `cratis new cratis --Framework
  net8.0`. Repeat a multi-value choice option to accumulate values. An unknown option is an error
  that names the valid set — never a silent drop.
- `--parameters` — list one project's own options (descriptions, data types, choices, defaults)
  instead of scaffolding.
- `--dry-run` — report what would be created and write nothing.
- `--force` — allow writing into a non-empty output directory.
- `--database` — the database backend for the scaffolded application: `mongodb` (the default),
  `postgresql`, `mssql` or `sqlite`, matched case-insensitively. A project that doesn't support
  database selection ignores the default and rejects an explicit selection with a named error
  rather than dropping it silently.

The full [project catalogue](templates.md) lists everything `cratis new` can create, with its
options.

## What happens under the hood

When you run `cratis new cratis -n MyApp`, the CLI resolves the project, scaffolds it into
`MyApp`, adds the package references your language and database selection require with concrete
resolved versions, restores packages when a build toolchain is available, and prints
getting-started instructions for exactly what you scaffolded. Then it finishes the project in
place: the Cratis AI configuration is synchronized — the same work `cratis ai update` does — so
the project leaves the scaffold with its rules, skills, and agent integration already set up,
with no follow-up command to remember. The created folder is the working directory for
everything after creation.

## Scripts and the --allow-scripts contract

Some projects install frontend dependencies as part of scaffolding. Running scripts on someone's
machine is not something a CLI does silently. The policy is explicit:

```bash
--allow-scripts yes     # run scripts
--allow-scripts no      # decline them; the scaffold still completes and the summary says so
--allow-scripts prompt  # ask before each script (interactive terminals only)
```

The default is `no`. When standard input is not a TTY, `prompt` is an error — the CLI refuses to
either silently run or silently skip a script. Every prompt also has a non-interactive
equivalent: the option flags themselves, and `--no-prompts` for interactive option prompts.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The project was created (scripts that ran either succeeded or were reported as declined) |
| `1` | Creation failed, or a required step failed |
| `2` | The command could not run — unknown project, download failure, invalid arguments |

## Local and unpublished projects

Project authors can exercise a project before publishing it:

```bash
cratis new cratis --template-path ../Templates/nupkgs/Cratis.Templates.1.3.0.nupkg
cratis new cratis --template-path ../Templates/Templates/Cratis
```

`--template-path` accepts an unpacked package folder or a local `.nupkg`. `--package <id>` and
`--version <version>` select any other published package — `--version` also overrides the
catalogue pin for the built-in package.
