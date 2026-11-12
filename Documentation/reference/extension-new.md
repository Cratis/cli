---
title: "cratis extension new: create an extension package that builds and verifies"
description: Create a full extension (an OCI artifact with an optional backend in C#, Kotlin, Go or TypeScript and an optional frontend) or a frontend-only npm component package, with an interactive wizard or flags, from the Cratis.Templates.Extensions templates.
---

`cratis extension new` creates an extension package in one command. What it creates already has a
[`contract.play`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md) with its manifest,
builds, and passes [`cratis extension verify`](extension-verify.md) without edits. See
[Manage extension packages](extension.md) for the other `cratis extension` commands.

```bash
cratis extension new [-n <NAME>] [--kind extension|package]
    [--backend csharp|kotlin|go|typescript|none] [--frontend react|mount|screenplay|none]
    [--component react|mount] [--display-name <TEXT>] [--description <TEXT>] [--publisher <NAME>]
    [-o <PATH>] [--template-path <PATH>] [--package <ID>] [--version <VERSION>]
    [--dry-run] [--force] [--no-prompts] [--format table|json]
```

## Two kinds of package

| Kind | Published as | Choices |
| --- | --- | --- |
| `extension` (the default) | An OCI artifact: the contract, an optional backend and an optional frontend, at fixed paths. | `--backend` is `csharp`, `kotlin`, `go`, `typescript` or `none`. `--frontend` is `react`, `mount`, `screenplay` (a Screenplay-authored component) or `none`. |
| `package` | An npm package with frontend components only. | `--component` is `react` or `mount`, and is required. A frontend-only package has no backend. |

Every combination is valid, including an extension with no backend and no frontend that only declares abilities.

## What every package contains

- `contract.play` with its manifest: display name, description, icon, marketplace images and requirements.
- A placeholder icon and image.
- For each frontend component, a static HTML placeholder with no scripts and no external files, which is what
  [`cratis extension verify`](extension-verify.md#the-checks) requires of every placeholder.
- A sample ability or component, with specifications.
- A README and the build and publish scripts.

## The wizard and the flags

Run the command with no arguments in a terminal and it asks only what the flags did not answer: the name, the kind, the
backend and frontend (or the component), the display name, the description and the publisher. A flag skips its question,
so a partly answered command line asks the rest.

The wizard and the flags build the same request and check it with the same rules, so they create the same package:

```bash
cratis extension new -n Stripe --backend go --frontend react --publisher acme
```

creates what answering `Stripe`, `extension`, `go`, `react` and `acme` in the wizard creates. `--no-prompts` never asks,
and neither does a run with redirected input or output or inside an AI agent: it uses the flags and the defaults
(a full extension, no backend, no frontend, the display name spelled out from the name).

| Option | Meaning |
| --- | --- |
| `-n`, `--name` | The package name, a PascalCase identifier such as `Stripe` or `OrderTracking`. |
| `--kind` | `extension` (default) or `package`. |
| `--backend`, `--frontend` | The parts of a full extension. Default `none`. |
| `--component` | The component kind of a frontend-only package. |
| `--display-name`, `--description` | The manifest text. Default: the name spelled out, and `The <name> extension`. |
| `--publisher` | The OCI namespace or npm scope. Default `example`. |
| `-o`, `--output` | The folder to create. Default: a folder named after the package. |
| `--template-path` | Use the templates in a local folder instead of the template package. |
| `--package`, `--version` | Use another template package, or a version of it. Default: `Cratis.Templates.Extensions`, latest. |
| `--dry-run`, `--force` | List the files without writing them; write into a folder that is not empty. |
| `--format` | `table` (default) or `json`: the kind, name, output folder and created files. |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The package was created (or listed, with `--dry-run`). |
| `1` | Creation failed after the template was found. |
| `2` | The request is invalid, or the template package or template could not be found or does not offer a choice. |

## The template interface

The CLI does not generate files itself. It instantiates templates from the
[Cratis/Templates](https://github.com/Cratis/Templates) package `Cratis.Templates.Extensions` through the same templating engine
as [`cratis new`](../new/index.md), so `dotnet new` can use them too. The agreement between the two:

| | Full extension | Frontend-only package |
| --- | --- | --- |
| Template short name | `cratis-extension` | `cratis-extension-package` |
| Name | The template's `sourceName`, replaced by the package name. | The same. |
| Choice symbols | `Backend` (`csharp`, `kotlin`, `go`, `typescript`, `none`), `Frontend` (`react`, `mount`, `screenplay`, `none`). | `Component` (`react`, `mount`). |
| Text symbols | `DisplayName`, `Description`, `Publisher`. | The same. |

The package folder also holds a `.extensionignore` that names what the build does not ship (sources, specifications, scripts), so that
[`cratis extension verify`](extension-verify.md#what-ships-and-extensionignore) can tell them from what is shipped.

The CLI passes every symbol for the kind, always, and fails with exit code `2` when a template does not declare one or does
not offer a value, so a template and the CLI cannot drift apart silently. The contract and manifest are at the root of the
created folder, and the container build puts them at `/extension/contract.play` as the
[contract](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-contract-file) requires.
The [templates README](https://github.com/Cratis/Templates/blob/main/README.md) lists them.

## Related

- [`cratis extension verify`](extension-verify.md): check what was created.
- [Manage extension packages](extension.md): restore, update and vendor the packages a model references.
- [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md): what `contract.play` declares.
- [Extension packages in Stage](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md): how a package is admitted when it is loaded.
