---
description: Use cratis chronicle to inspect event history, diagnose failed processing, and find commands for event stores, observers, projections, and read models.
---

# Chronicle

Use `cratis chronicle` to inspect event history and processing state on a Chronicle server, then choose a recovery command when you know what failed. It connects to Chronicle's management APIs and authenticates when required. Some commands list server-wide resources; others work within a selected event store and namespace.

If you have not connected yet, complete [Getting started](/cli/getting-started/) first. This page helps you choose a command group and set its target; the individual reference pages describe each operation.

## What should I inspect first?

Start by listing the event stores on the server. Before using `diagnose`, confirm the saved event store and namespace, or pass `-e` and `-n` to select them explicitly. The report checks connection and processing health for that target:

```bash
cratis chronicle event-stores list
cratis chronicle diagnose
```

An observer handles recorded events to build read models or run application behavior. If a read model looks stale, inspect observer state and failed partitions before deciding to replay it. A failed partition records the part of an observer's work that stopped. Follow [Find and fix a stuck observer](../scenarios/fix-a-stuck-observer.md) to read that failure and choose the next step.

Inspection does not authorize a mutation. Before replay, retry, recovery, or administration commands, confirm the server, event store, namespace, target, and operational procedure.

## Connection Flags

Most commands accept the following connection flag:

| Flag | Description |
|---|---|
| `--server <CONNECTION_STRING>` | Chronicle server connection string. Overrides the active context and the `CHRONICLE_CONNECTION_STRING` environment variable. |

Without `--server`, the CLI checks `CHRONICLE_CONNECTION_STRING`, then the active context, then the local default `chronicle://localhost:35000`. For credentials and the full connection string format, see the [Connection](../reference/connection.md) reference page. Connection strings and context files can contain credentials; keep them out of source control and shared logs.

## Event Store and Namespace Flags

An event store holds an application's event history; a namespace selects an isolated scope within it. Commands that work at either level accept these flags. Check both values when switching environments, because changing the server alone does not identify the data you intend to inspect:

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store name. When omitted, falls back to the active context's event store, then `default`. An explicit value always wins — including `default` itself. |
| `-n, --namespace <NAME>` | Namespace within the event store. When omitted, falls back to the active context's namespace, then `Default`. An explicit value always wins — including `Default` itself. |

## Sub-Commands

Choose the group for the evidence you need: `events` for recorded history, `observers` and `failed-partitions` for processing, or `read-models` for derived state. The table also includes state-changing and administrative groups; read their operation-specific guidance before using them.

| Sub-command | Description |
|---|---|
| `event-stores` | List event stores registered on the server. |
| `namespaces` | List namespaces within an event store. |
| `event-types` | List and inspect registered event type definitions. |
| `events` | Query events from an event sequence or retrieve the tail sequence number. |
| [`streams`](streams.md) | List closed stream scopes and repair an exact manual closure. |
| `observers` | List, inspect, replay, retry, and clear quarantine for observers. |
| `subscriptions` | Manage cross-event-store subscriptions for event forwarding. |
| `failed-partitions` | List and inspect partitions where an observer has failed. |
| `projections` | List and inspect projection definitions. |
| `read-models` | List, query, and inspect read model instances and snapshots. |
| `recommendations` | List, perform, and ignore server-generated recommendations. |
| `identities` | List principals that have interacted with the event store. |
| `jobs` | List, inspect, resume, and stop long-running server-side jobs. |
| `auth` | Check authentication status. |
| `applications` | Manage OAuth client applications authorized to connect to Chronicle. |
| `users` | Manage human users who can log in to Chronicle. |
| `diagnose` | Run a health check against the connected Chronicle server. |

## Global Flags

All `cratis chronicle` commands also inherit the top-level global flags:

| Flag | Description |
|---|---|
| `-o, --output <FORMAT>` | Output format: `table`, `plain`, `json`, `json-compact`. |
| `-q, --quiet` | Output only key identifiers, one per line. Useful for piping. |
| `-y, --yes` | Skip confirmation prompts. |
| `--debug` | Enable verbose debug output. |

For a guided investigation, continue with the [operational scenarios](../scenarios/index.md). For scripts, select an explicit output format and bind parsing to your CLI version; ordinary output formats are not an unversioned stable machine contract.
