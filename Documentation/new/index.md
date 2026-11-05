---
title: Create a project with Cratis CLI
description: Use cratis new to choose a project template, language, and database, scaffold an application, and synchronize its configured Cratis AI guidance.
---

`cratis new` creates the files for a Cratis application from a project template. Choose the
project, language, and database in the wizard, or pass those choices as options when you need a
repeatable command. This is the project-creation workflow; you do not need a running Chronicle
store to begin.

[Install the CLI](/cli/getting-started/#install-it) first. The commands below show the wizard,
the template list, and direct creation for each language. Choose the one that fits your task:

```bash
cratis new
cratis new list
cratis new --language csharp -n MyApp -o MyApp
cratis new --language kotlin -n MyApp -o MyApp
cratis new --language java -n MyApp -o MyApp
```

In the wizard, choose a project first. The default is `cratis`, the full-stack web application.
The language and database questions offer only the choices that project supports; a question
with a single choice is skipped. `cratis new list` shows the available templates without creating
anything. The three language-specific commands create the `cratis` web application directly.

After creation, read the summary before starting the application. It reports declined scripts,
failed setup steps, and the result of synchronizing any Cratis AI configuration supplied by the
template. The generated files can exist even when one of those steps needs attention.

## Languages

The CLI recognizes these language selections. Use `cratis new list` to check which languages
the selected project offers:

| Language | Flag |
| --- | --- |
| C# | `--language csharp` (`c#` is also accepted) |
| Kotlin | `--language kotlin` |
| Java | `--language java` |

`--language` is required when scaffolding directly. A language option selects a template
variant; it does not establish equivalent framework or client capabilities across C#, Kotlin,
and Java. Check the chosen template and its generated instructions for the application shape
and toolchain. The [project catalogue](templates.md) lists the available project shapes and options.

## How it reads

- `cratis new` — the creation wizard: which project (default `cratis`), which language, which
  database. Non-interactive terminals get guidance instead of a hang; `--no-prompts` forces the
  non-interactive behavior everywhere.
- `cratis new list` — list what's available, with each entry's supported languages and
  databases. A project offered in more than one language (C#, Kotlin, and Java `cratis`)
  collapses into a single entry; the language question (or `--language`) picks the runtime.
- `--language` — required when scaffolding directly. Selects the language variant from
  its template package: `csharp` (C#; `c#` is accepted), `kotlin`, or `java`. Omitting the
  project name selects `cratis` in the requested language. Values are matched case-insensitively;
  an unsupported value is an error, and if the package lacks that template, the error lists
  projects available in that language instead of falling back to C#.
- `cratis new <project> --language <name>` — scaffold a specific project for that language, e.g.
  `cratis new cratis-aspire --language csharp`.
- `-n/--name` — the project name. Defaults to the project's own default, falling back to the
  output folder name.
- `-o/--output` — the output directory. This is the one place `cratis new` deliberately diverges
  from the rest of the CLI: everywhere else `-o` selects the output *format*, but here it is the
  output *directory*. Use `--format json` for machine-readable output.
- Project-specific options are passed as `--<Option> <value>`, e.g. `cratis new cratis --language csharp
  --Framework net10.0`. Repeat a multi-value choice option to accumulate values. An unknown option is an error
  that names the valid set.
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

When you run `cratis new --language csharp -n MyApp`, the CLI selects the template and writes the
application into `MyApp`. It adds the package references required by your language and database
choices with concrete resolved versions. Package restore runs when a build toolchain is
available, and the CLI prints getting-started instructions for the application it created.

If the new project contains `.cratis/ai.json`, the CLI then attempts the same guidance
synchronization as `cratis ai update`. Projects without that configuration skip this step.
Conflicts or failures leave the created project in place and appear in the summary. Resolve the
reported issue, then run `cratis ai update` inside the new project to retry or refresh the
guidance later. The CLI uses that folder for this finishing step; your calling shell stays in
its original directory.

## Scripts and the --allow-scripts contract

Some templates use scripts to install frontend dependencies. Decide whether those scripts may
run before creating the project, especially in automation:

```bash
--allow-scripts yes     # run scripts
--allow-scripts no      # decline them; the scaffold still completes and the summary says so
--allow-scripts prompt  # ask before each script (interactive terminals only)
```

The default is `no`, so creating the files does not imply the scripts ran. Choose `yes` only
when you intend to run them, or `prompt` to decide for each script in an interactive terminal.
If standard input is not a terminal (TTY), `prompt` is an error; use `yes` or `no` explicitly.
Pass option flags for repeatable choices and `--no-prompts` to disable interactive option prompts.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The project was created (scripts that ran either succeeded or were reported as declined) |
| `1` | Creation failed, or a required step failed |
| `2` | The command could not run — unknown project, download failure, invalid arguments |

## Local and unpublished projects

Project authors can exercise a project before publishing it:

```bash
cratis new cratis --language csharp --template-path ../Templates/nupkgs/Cratis.Templates.1.7.0.nupkg
cratis new cratis --language csharp --template-path ../Templates/Templates/Cratis
```

`--template-path` accepts an unpacked package folder or a local `.nupkg`. `--package <id>` and
`--version <version>` select any other published package — `--version` also overrides the
installed template version for a single run. Where the installed templates come from, and how
`cratis update` refreshes them, is described in the
[template catalogue](templates.md#where-templates-come-from).

## Continue with the created project

Start with the creation summary: address declined scripts or failed setup steps, then follow
the generated project's getting-started instructions. Those instructions match the language
and database you chose. To compare another project shape or inspect its parameters before
creating it, open the [project catalogue](templates.md). Once you have a running Chronicle store,
the [CLI connection tutorial](/cli/getting-started/) takes you into terminal inspection.
