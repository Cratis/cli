---
title: Template catalogue
description: Every template cratis new offers, with its parameters, choices and defaults.
---

The CLI ships a programmatic catalogue pinned to a published version of
[Cratis.Templates](https://www.nuget.org/packages/Cratis.Templates) — `1.3.0` at the time of
writing. The pin makes scaffolding reproducible; `--version` overrides it when you want a
different one:

```bash
cratis new cratis --version 1.2.2
```

All four templates come from the same package, are written in C#, and share the same parameter
conventions. List them any time with `cratis new`; inspect one template's parameters with
`cratis new <template> --parameters`.

## cratis — Cratis Web Application

The full-stack starting point: a Cratis web application with an Arc backend, a React frontend
built with Vite, and the Cratis AI configuration wired in.

```bash
cratis new cratis -n MyApp -o MyApp
```

| Parameter | Type | Choices | Default | Description |
| --- | --- | --- | --- | --- |
| `--Framework` | choice | `net8.0`, `net9.0`, `net10.0` | `net10.0` | Target framework |
| `--packageManager` | choice | `yarn`, `pnpm`, `npm`, `none` | `yarn` | Package manager for frontend dependencies |

The template adds the `Cratis` and `Cratis.Arc.MongoDB` package references with versions
resolved at scaffold time, restores packages when dotnet is available, installs frontend
dependencies through your chosen package manager (a script post action — see the
[--allow-scripts contract](index.md#scripts-and-the---allow-scripts-contract)), and prints
getting-started instructions. Choose `none` to skip frontend tooling entirely.

```bash
cratis new cratis -n MyApp --Framework net8.0 --packageManager none --allow-scripts no
```

## cratis-aspire — Cratis Aspire Application

The web application with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) orchestration:
a solution with composition and infrastructure projects, the Aspire dashboard, and the full
observability stack.

```bash
cratis new cratis-aspire -n MyApp -o MyApp
```

| Parameter | Type | Choices | Default | Description |
| --- | --- | --- | --- | --- |
| `--Framework` | choice | `net8.0`, `net9.0`, `net10.0` | `net10.0` | Target framework |

The scaffold is a multi-project solution; each project's package references are resolved to
concrete versions, so the output is buildable the moment restore runs.

## cratis-chronicle-console — Cratis Chronicle Console

The smallest starting point: a console application connected to a Chronicle event store, with a
docker-compose file for the server.

```bash
cratis new cratis-chronicle-console -n MyApp -o MyApp
```

| Parameter | Type | Choices | Default | Description |
| --- | --- | --- | --- | --- |
| `--Framework` | choice | `net8.0`, `net9.0`, `net10.0` | `net10.0` | Target framework |

## cratis-chronicle-web — Cratis Chronicle Web

A minimal web application connected to a Chronicle event store — the console template's web
sibling.

```bash
cratis new cratis-chronicle-web -n MyApp -o MyApp
```

| Parameter | Type | Choices | Default | Description |
| --- | --- | --- | --- | --- |
| `--Framework` | choice | `net8.0`, `net9.0`, `net10.0` | `net10.0` | Target framework |

## Database selection

`--database` selects the database backend the scaffolded application uses:

```bash
cratis new cratis -n MyApp --database postgresql
```

The value is matched case-insensitively against the supported backends — `mongodb`, `postgresql`,
`mssql` and `sqlite` — and defaults to `mongodb`. It is applied as the template's `Database`
parameter, so a template authors its database support the way it authors any parameter:
choices on the parameter constrain the offered backends, conditions switch on it
(`#if (Database == PostgreSQL)` with quoteless literals enabled), and switch or derived symbols
derive values from it. When the parameter declares choices, the selection is injected in the
choice's canonical casing.

The templates in the catalogue today target MongoDB. A template that declares no `Database`
parameter ignores the default and rejects an explicit `--database` with an error naming the
template, so an unsupported selection is never silently dropped.

## Templates from other packages

The catalogue is the curated default, not a boundary. Any package that follows the same layout —
a `.template.config/template.json` at any depth — can be instantiated:

```bash
cratis new <short-name> --package <package-id> --version <version>
cratis new <short-name> --template-path <folder-or-nupkg>
```

Local folder feeds in your `NuGet.Config` are honored the same as nuget.org, which makes
inner-loop iteration on templates straightforward: pack locally, point a feed at the output,
scaffold, repeat.
