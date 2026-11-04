---
title: Diagnose
description: Check Chronicle health, identify incomplete checks, and inspect findings across stores and namespaces.
---

Run `diagnose` to find out whether Chronicle is reachable and which observers or partitions need attention. It reports the server version, event stores, observer states, failed partitions, recommendations, and event log tail. Use a one-time report for an investigation or a scheduled health check.

Read `checksComplete` and `checksCouldNotRun` before interpreting zero counts. A query that failed tells you what could not be checked, including its name, store, namespace, and exception message. A successful query can return no observers or failures; that is an empty result, not a failed check. The report is healthy only when the server is reachable, every diagnostic query completed, and there are no failed partitions or quarantined observers. Even then, it does not prove that a particular view is current.

## Choose one inspection scope

Use the [first-inspection tutorial](../getting-started/index.mdx) to select a server, event store, and namespace you are authorized to inspect. Pass `--server`, `-e`, and `-n` explicitly if context or environment defaults are uncertain. The report describes that selection; it cannot establish that an expected projection registered.

Start with one sweep. If it finds nothing, check the intended target before asking to inspect other stores or namespaces. For a returned failure, use [failed-partitions show](failed-partitions.md#show) to read the error for that observer and partition. Review a returned suggestion through [recommendations list](recommendations.md#list); its presence does not authorize the action. [Find and fix a stuck observer](../scenarios/fix-a-stuck-observer.md) connects those findings to a cause investigation and an approved recovery.

Use `--watch` only when you have agreed how long it may run, how many refreshes to make, and who will stop it. It has no built-in iteration limit. For assistant-driven inspection, prefer a one-shot report and stop after the agreed number of requests.

## What is checked

| Check | What is reported and how it affects health |
|---|---|
| Connection | Whether the management API responds. Unreachable means unhealthy. |
| Server version | The current server version and, when available, a newer version from NuGet. An update is advisory; an unavailable package feed does not make Chronicle unhealthy. |
| Event stores | Available store names. A query failure makes the report incomplete. |
| Namespaces | Namespace discovery when either aggregation option is used. A discovery failure or no scopes to check makes the report incomplete. |
| Observers | Counts of active, replaying, suspended, disconnected, and quarantined observers. Any quarantined observer means unhealthy. Suspended or disconnected observers alone do not change the exit code. |
| Failed partitions | Count and observer/partition identities. Any failed partition means unhealthy. |
| Recommendations | Pending recommendation count and identities. Pending recommendations alone do not change the exit code. |
| Event sequence tail | The event log's highest sequence number. A completed query with no events is shown as `empty` and is not itself unhealthy; a failed query is shown as `could not check`. |

A failure to run the observers, failed-partitions, recommendations, or event-sequence query makes the report incomplete and unhealthy, even when the displayed known counts are zero. Server-reported query errors are treated the same way as thrown exceptions. Other scopes continue to be checked after one fails.

## Usage

```bash
cratis chronicle diagnose
```

By default, the command checks the selected event store and namespace, resolved from `--event-store` / `--namespace`, the active context, then the built-in defaults (`default` / `Default`). It does not change server state.

## Options

| Flag | Description |
|---|---|
| `--all-namespaces` | Discover and check every namespace in the selected event store. Overrides `--namespace`. |
| `--all-event-stores` | Discover every event store and check all of their namespaces. Implies `--all-namespaces` and overrides `--event-store` and `--namespace`. |
| `--watch` | Continuously refresh the report. Press Ctrl+C to stop. Requires table output format. |
| `--interval <SECONDS>` | Refresh interval in seconds when using `--watch`. Defaults to `5`; must be at least `1`. |

Aggregated reports include totals and identify the event store and namespace of each quarantined observer, failed partition, recommendation, or unavailable check. Event log tails belong to individual namespaces, so they are reported per scope rather than summed. Successfully read tails remain visible even when discovery of another scope fails. Failure to discover stores or namespaces makes the report incomplete; dependent checks are reported as skipped with the discovery reason, not as passed. An empty discovery result also cannot produce a healthy sweep.

## Output

- Table/text and watch reports show quarantined observer counts and a separate **Could not check** section with reasons. Unavailable checks are not marked as passed.
- Plain output includes `healthy`, `checks_complete`, `checks_could_not_run`, and `observers_quarantined`. Repeated `could_not_check` and `finding` lines name the event store and namespace. Values containing whitespace, `=`, `"`, or `\` are double-quoted, with quotes and backslashes escaped as `\"` and `\\`; CR and LF become spaces. See [Output Formats](../reference/output-formats.md#plain).
- JSON includes `healthy`, `checksComplete`, `checksCouldNotRun`, `findings`, and `observers.quarantined`. `observers.total` counts every observer returned by completed queries, including `Unknown` and `Quarantined` states, both in the aggregate and in each scope. `scopes` contains the individual namespace summaries. When a check is incomplete, counts represent only the results that were successfully retrieved, not a verified absence of problems.

In aggregated reports, the top-level tail is null, so JSON omits `eventSequenceTail` and the plain `event_sequence_tail=` value is empty, even if only one namespace is discovered. Read tails from JSON `scopes` or plain `scope_event_sequence_tail` records instead. Text and watch reports list per-scope tails, and plain output emits `scope_event_sequence_tail` records, only for aggregated sweeps. Non-aggregated text, watch, and plain reports show only the selected namespace's top-level tail. JSON always includes the checked namespaces in `scopes`, including the selected namespace in a non-aggregated sweep.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Complete, healthy sweep: reachable server, no quarantined observers, and no failed partitions. |
| `3` (`server_error`) | Unhealthy or incomplete sweep. Inspect `checksCouldNotRun` / `checks_complete` to distinguish unavailable checks from detected problems. No dedicated could-not-check code exists in the CLI taxonomy. |
| `2` (`connection_error`) | Connection setup failed before the diagnostic sweep could run. |
| `4` (`authentication_error`) | Authentication failed before the sweep could run. |
| `5` (`validation_error`) | Invalid settings, such as a watch interval below one second or non-table watch output. |

Until the first sweep completes, every watch check row shows a neutral **checking…** state. The header identifies all stores or namespaces when their aggregation flags are set. When watch mode stops, it returns the last sweep's exit code (`3` if no sweep completed). Use one-time mode for scheduled checks.

## Examples

Run a one-time health check:

```bash
cratis chronicle diagnose
```

Run against a specific server:

```bash
cratis chronicle diagnose --server chronicle://prod.example.com:35000
```

For an investigation authorized across every namespace in one store:

```bash
cratis chronicle diagnose --event-store MyStore --all-namespaces -o json
```

If the investigation covers every store and namespace:

```bash
cratis chronicle diagnose --all-event-stores -o json
```

Get machine-readable output for CI:

```bash
cratis chronicle diagnose -o json
echo "Exit code: $?"
```

For an agreed monitoring period, refresh every five seconds and stop with Ctrl+C:

```bash
cratis chronicle diagnose --watch
```

For monitoring authorized across all namespaces, use a custom interval:

```bash
cratis chronicle diagnose --all-namespaces --watch --interval 10
```

For a CI health check authorized across all stores, fail the build if the sweep is unhealthy or incomplete. This checks Chronicle health, not a specific application's expected model state:

```bash
cratis chronicle diagnose --all-event-stores -o json --yes
if [ $? -ne 0 ]; then
  echo "Chronicle health check failed or could not complete"
  exit 1
fi
```
