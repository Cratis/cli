---
title: Creating projects with cratis new
description: Scaffold Cratis applications from templates with no .NET SDK required — listing, instantiating, and what happens under the hood.
---

You want to start a new Cratis application. Today that means either cloning a sample and renaming
things by hand, or installing the .NET SDK and using `dotnet new` — which is fine if you have the
SDK, and a detour if you don't. `cratis new` removes the detour: the CLI ships its own
template engine that reads the same `template.json` format `dotnet new` uses, so the templates
already published as `Cratis.Templates` work unchanged — and they work on a machine with no
.NET installed at all.

```bash
cratis new
cratis new --language csharp -n MyApp -o MyApp
```

The first command lists the available templates. The second scaffolds the Cratis web application
into a `MyApp` folder. No SDK, no `dotnet new install`, no package manager until you choose one.

## How it reads

`cratis new` follows `dotnet new` semantics because compatibility is the point:

- `cratis new` — list the templates the CLI offers.
- `--language` — **required when instantiating.** The language selects the template package and
  the template used when none is named: `csharp` (C#; `c#` is accepted) instantiates the `cratis`
  template today, and `kotlin` and `java` light up their `cratis-kotlin` and `cratis-java`
  template packages as those ship. Matched case-insensitively; a language whose package is not
  published yet is a named error, never a silent fallback to C#.
- `cratis new <template> --language <name>` — instantiate a specific template from the language's
  package, e.g. `cratis new cratis-aspire --language csharp`.
- `-n/--name` — the project name. Defaults to the template's own default, falling back to the
  output folder name.
- `-o/--output` — the output directory. This is the one place `cratis new` deliberately diverges
  from the rest of the CLI: everywhere else `-o` selects the output *format*, but here it is the
  output *directory*, exactly like `dotnet new`. Use `--format json` for machine-readable output.
- Template parameters are passed as `--<Parameter> <value>`, e.g.
  `cratis new cratis --Framework net8.0`. Repeat a multi-value choice parameter to accumulate
  values. An unknown parameter is an error that names the valid set — never a silent drop.
- `--parameters` — list one template's own parameters (descriptions, data types, choices,
  defaults) instead of instantiating.
- `--dry-run` — report what would be created and write nothing.
- `--force` — allow writing into a non-empty output directory.
- `--database` — the database backend for the scaffolded application: `mongodb` (the default),
  `postgresql`, `mssql` or `sqlite`, matched case-insensitively. The selection becomes the
  template's `Database` parameter, so the template uses it like any parameter — driving package
  references, connection settings and conditionals. Templates that declare no `Database`
  parameter ignore the default and reject an explicit selection with a named error rather than
  dropping it silently.

The full [template catalogue](templates.md) lists every template with its parameters.

## No .NET required

The engine is self-contained. Acquiring the template package from nuget.org, rendering it, and
finishing it with post actions — including resolving `Version="*"` package references to
concrete published versions — is done by the CLI itself. A machine with nothing but `cratis`
installed produces a complete, buildable project with real package versions.

There is exactly one sanctioned exception: the *restore* post action. If `dotnet` happens to be
on your PATH the CLI runs `dotnet restore` for you; if it is not, the scaffold still completes
and the summary tells you to run restore yourself once you have an SDK. Restoring packages is a
build-time concern — creating the project is not.

## What happens under the hood

When you run `cratis new cratis -n MyApp`:

1. The CLI resolves `Cratis.Templates` from your NuGet configuration (repository-level
   `NuGet.Config` if present, then your user configuration, then nuget.org) and downloads the
   package version pinned in its catalogue. Local folder feeds work too — the CLI reads the
   `.nupkg` files directly.
2. The package is extracted into the CLI's own template store under `~/.cratis/templates` —
   isolated from `dotnet new`'s store. `cratis new` never changes what `dotnet new list` prints,
   and vice versa. A cached package resolves offline.
3. The template's `template.json` drives everything: parameters are bound from your command
   line (prompts appear interactively for parameters that declare one; `--no-prompts` always
   takes defaults), symbols are resolved in dependency order, and every file is processed —
   conditional directives evaluated, tokens replaced, names propagated into file paths.
4. Post actions finish the project natively: package references are added to the project file
   with versions resolved through the same NuGet configuration, and getting-started instructions
   are printed.

## Scripts and the --allow-scripts contract

Some templates include script post actions — installing frontend dependencies, for example.
Running scripts on someone's machine is not something a CLI does silently. The policy is
explicit:

```bash
--allow-scripts yes     # run script post actions
--allow-scripts no      # decline them; the scaffold still completes and the summary says so
--allow-scripts prompt  # ask before each script (interactive terminals only)
```

The default is `no`. When standard input is not a TTY, `prompt` is an error — the CLI refuses to
either silently run or silently skip a script. Every prompt also has a non-interactive
equivalent: the parameter flags themselves, and `--no-prompts` for the template's interactive
parameter prompts.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The template was created (post actions that ran either succeeded or were reported as declined) |
| `1` | Creation failed, or a post action marked `continueOnError: false` failed |
| `2` | The command could not run — unknown template, package acquisition failure, invalid manifest, invalid arguments |

These follow the CLI's own exit-code contract rather than `dotnet new`'s numeric codes
(`73`, `100`, `102`, …).

## Relationship to dotnet new

The templates are the same and the format is the same — both doors lead to the same room:

```bash
dotnet new install Cratis.Templates
dotnet new cratis -n MyApp -o MyApp
```

still works and remains supported. Use `cratis new` when you want scaffolding without an SDK;
use `dotnet new` when you already live in the SDK. The two stores are isolated — installing a
template for one does not affect the other.

## Local and unpublished templates

Template authors can exercise a package before publishing it:

```bash
cratis new cratis --template-path ../Templates/nupkgs/Cratis.Templates.1.3.0.nupkg
cratis new cratis --template-path ../Templates/Templates/Cratis
```

`--template-path` accepts an unpacked package folder or a local `.nupkg`. `--package <id>` and
`--version <version>` select any other published package — `--version` also overrides the
catalogue pin for the built-in package.
