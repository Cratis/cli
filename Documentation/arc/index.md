---
title: Arc endpoint discovery
description: Discover commands and queries on a running Arc application's HTTP endpoints without executing them or selecting a Chronicle context.
---

You have a running application and need to know which command and query routes it
registered. `cratis arc` reads the application's introspection metadata. It does
**not** execute the listed commands or queries, and a successful list does not
prove their business behavior.

## Before you connect

Install the CLI through [Getting started](../getting-started/index.mdx), and
obtain an authorized base URL for the running Arc application. The application
must map its introspection endpoints with `MapIntrospectionEndpoints()` during
startup; the CLI does not add them or start the application.

The Arc application is a separate HTTP host from Chronicle's management server.
A Chronicle context, event store, or namespace does not select the Arc host.
You do not need Chronicle credentials just to select an Arc URL; the application
owner determines its endpoint access requirements.

## Discover the application's endpoints

Substitute the authorized base URL if it isn't the local example:

```bash
cratis arc commands list --url http://localhost:5000 --output plain
cratis arc queries list --url http://localhost:5000 --output plain
```

Each successful request returns metadata rows for that endpoint kind: command
names/namespaces, routes, and summaries; query rows also include type. Use
`-o json` for the structured metadata. Inspect one catalog before requesting the
other if that answers your question; neither list supports pagination.

An empty successful response means no endpoints of that kind were returned.
A connection error means the application could not be reached; a non-success
HTTP response means discovery failed. Check the selected URL, application
startup, endpoint mapping, and access policy with its owner. Do not interpret
those failures as an empty catalog or try Chronicle replay to fix them.

## URL resolution

| Priority | Source |
| --- | --- |
| 1 | Explicit `--url <URL>` |
| 2 | `ARC_URL` environment variable |
| 3 | `applicationUrl` in `Properties/launchSettings.json` or `properties/launchSettings.json` |
| 4 | `http://localhost:5000` |

Launch-settings lookup starts in the current directory and walks parents. It
uses the first usable profile URL, taking the first URL in a semicolon-separated
value. It does not start that profile. Pass `--url` when the working directory or
environment could otherwise select the wrong application.

## Command reference

| Sub-command | Metadata requested |
| --- | --- |
| [commands list](commands.md) | `GET /.cratis/commands`; registered command endpoints. |
| [queries list](queries.md) | `GET /.cratis/queries`; registered query endpoints. |

Both accept `--url` and the global flags:

| Flag | Description |
| --- | --- |
| `-o, --output <FORMAT>` | `table`, `plain`, `json`, or `json-compact`. |
| `-q, --quiet` | Key identifiers, one per line. |
| `--debug` | Debug information to stderr; review for sensitive information before sharing. |

Discovery is read-only. Any later HTTP call to an application route is a separate
operation with its own authorization, inputs, and possible effects—not an implied
next step. For an event/read-model investigation, return to the
[Chronicle workflow](../chronicle/index.md).
