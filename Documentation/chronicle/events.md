---
title: Events
description: Query events from a Chronicle event sequence and read its tail sequence number with cratis chronicle events, to verify appends and inspect content.
---

The `events` commands let you query events from an event sequence and retrieve the highest used sequence number. Use these commands to verify that events were appended, inspect their content, and understand the current state of an event log.

For a first investigation, use [Verify events were appended](../scenarios/verify-events-were-appended.md). Select an authorized server, event store, namespace, and event source before reading payloads; they may contain sensitive application data.

## get

Retrieves events from an event sequence, with optional filtering by range, event source, and event type.

```bash
cratis chronicle events get --from 42000 --to 42020
```

`--to` limits output only: the CLI retrieves events from `--from` onward before applying the upper bound locally. Even a small output interval can retrieve all remaining matching events in the log. Neither `--to` nor a source/type filter is a hard network or server-work budget.

Before using any example below, replace `42000` and `42020` with an approved recent interval near the observed tail in your authorized scope. Do not use an old interval or start at zero to explore a long log. If a hard retrieval budget is required, stop: this command has no count/limit option that enforces it.

Event type filters send identifiers only. Although `Name+generation` syntax is accepted, it does not constrain the returned generation. Inspect `generation` in JSON output to verify the expected contract.

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to query. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to query. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence to query. Defaults to the event log. |
| `--from <SEQUENCE_NUMBER>` | Start of the sequence range (inclusive, 0-based). Defaults to `0`. |
| `--to <SEQUENCE_NUMBER>` | Inclusive output upper bound, applied locally after retrieval. Does not bound the server request. |
| `--event-source-id <ID>` | Filter events to a single event source. |
| `--event-type <TYPES>` | Comma-separated event type identifiers; accepts `name+generation` syntax but sends identifiers only, without a generation filter. |

### Output tip

Use `--output plain` for concise event rows. Use `--output json` when you need payload/context fields or generation, keeping the same approved recent start and source filter. Output format and `--to` do not cap retrieval. Output size depends on the selected events; see [output formats](../reference/output-formats.md) rather than relying on a fixed size ratio.

### Examples

Select an output interval near the observed tail (replace these illustrative positions first):

```bash
cratis chronicle events get --from 42000 --to 42020
```

Filter to a specific event source within that output interval:

```bash
cratis chronicle events get --event-source-id "user-42" --from 42000 --to 42020
```

Filter to a single event type identifier:

```bash
cratis chronicle events get --event-type UserRegistered --from 42000 --to 42020
```

Inspect generations in JSON; the `+2` suffix does not filter to generation 2:

```bash
cratis chronicle events get --event-type UserRegistered+2 --from 42000 --to 42020 --output json
```

Filter to multiple event type identifiers:

```bash
cratis chronicle events get --event-type UserRegistered,UserEmailChanged --from 42000 --to 42020
```

Combine filters for targeted inspection:

```bash
cratis chronicle events get --event-source-id "user-42" --event-type UserRegistered --from 42000 --to 42020 --output json
```

## tail

Gets the highest used sequence number in an event sequence. This is the tail of the log — the sequence number of the last appended event. It is not a count of events: gaps may exist in the sequence if events were redacted or sequences were not used contiguously.

```bash
cratis chronicle events tail
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to query. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to query. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence to query. Defaults to the event log. |
| `--event-type <TYPE>` | Return the tail sequence number for a specific event type. |
| `--event-source-id <ID>` | Return the tail sequence number for a specific event source. |

A tail change means that the selected scope advanced, not that one particular application action caused it. Likewise, an observer subscribed to only some event types need not have a handled position equal to the global tail. Inspect the expected source, type, generation, and resulting read model before drawing that conclusion.

### Output

With `--output plain`, the command returns a single number on one line, making it easy to capture in a script:

```bash
TAIL=$(cratis chronicle events tail --output plain)
```

### Examples

Get the tail sequence number of the event log:

```bash
cratis chronicle events tail
```

Get the tail for a specific event source:

```bash
cratis chronicle events tail --event-source-id "user-42"
```

Get the tail for a specific event type:

```bash
cratis chronicle events tail --event-type UserRegistered
```

Capture the tail number in a script:

```bash
TAIL=$(cratis chronicle events tail -e myapp --output plain)
echo "Current tail: $TAIL"
```
