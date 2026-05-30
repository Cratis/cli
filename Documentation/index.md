---
title: Cratis CLI
description: A terminal window into a running Chronicle event store — inspect events, watch observers, browse read models, and diagnose issues without writing a line of code.
---

When something happens in a running Chronicle system — an event you need to see, an observer that's stuck, a read model that looks wrong, a partition that failed — you don't want to write a query or attach a debugger. You want to *look*. The **Cratis CLI** (`cratis`) is that look: a fast, terminal-first window into any [Chronicle](/chronicle/) event store, local or remote.

```bash
brew install cratis            # or: dotnet tool install -g Cratis.Cli
cratis get-started             # connects to your local Chronicle and shows you around
```

## What you can do with it

Point it at a Chronicle server and, straight from the terminal:

- **Inspect events** — browse [event types](/chronicle/concepts/event-type/), read the events on any stream, and follow appends as they happen.
- **Watch the machinery** — see [observers](/chronicle/concepts/observer-patterns/) (projections, reducers, reactors), check their state, and **diagnose a [failed partition](/chronicle/troubleshooting/)** when one pauses.
- **Browse read models** — look at projected state directly, without a database client.
- **Operate** — manage namespaces, users, identities, and applications.
- **Connect anywhere** — switch between local, staging, and production with named [contexts](context/index.md).

## Why a CLI?

You *could* open a database client, write ad-hoc queries, and reverse-engineer what your event store is doing. The CLI gives you a purpose-built, event-sourcing-aware view instead: it speaks events, streams, observers, and read models — so "why is this projection behind?" is one command, not an investigation.

It's also **AI-native**. Run [`cratis init`](getting-started/index.md) in your project and it teaches your AI tools (Claude Code, GitHub Copilot, Cursor, Windsurf) about your Chronicle store — writing a `CHRONICLE.md` command catalog and a `chronicle-diagnose` slash command, so your assistant can help operate the store too.

:::tip
The first time you run any `cratis` command it auto-creates a `default` context pointing at `chronicle://localhost:35000` — so against a local Chronicle, there's nothing to configure. Just install and go.
:::

## Where to go next

| Section | What's there |
|---|---|
| [Getting Started](getting-started/index.md) | Install, first run, shell completions, and AI-tool setup |
| [Context](context/index.md) | Named server connections for local / staging / production |
| [Chronicle commands](chronicle/index.md) | Every command for inspecting and operating a Chronicle store |
| [Arc commands](arc/index.md) | Commands for Arc applications |
| [Reference](reference/index.md) | Global options, output formats, and connection strings |
