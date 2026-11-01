---
title: Events
description: Query events from a Chronicle event sequence and read its tail sequence number with cratis chronicle events, to verify appends and inspect content.
---

The `events` commands let you query events from an event sequence and retrieve the highest used sequence number. Use these commands to verify that events were appended, inspect their content, and understand the current state of an event log.

For a first investigation, use [Verify events were appended](../scenarios/verify-events-were-appended.md). Select an authorized server, event store, namespace, and event source before reading payloads; they may contain sensitive application data.

## get

Retrieves events from an event sequence, with optional filtering by range, event source, and event type.

```bash
cratis chronicle events get --from 0 --to 100
```

Without `--to`, the request has no explicit upper sequence bound. A source or type filter alone does not bound that range. Choose a small interval around the action you are investigating. Event type filters default to generation 1 unless you specify `Name+generation`; an empty result can reflect the generation rather than an absent append.

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to query. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to query. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence to query. Defaults to the event log. |
| `--from <SEQUENCE_NUMBER>` | Start of the sequence range (inclusive, 0-based). Defaults to `0`. |
| `--to <SEQUENCE_NUMBER>` | End of the sequence range (inclusive). Omit to read to the current tail. |
| `--event-source-id <ID>` | Filter events to a single event source. |
| `--event-type <TYPES>` | Comma-separated list of event type names or `name+generation` pairs to filter by. |

### Output tip

Use `--output plain` for concise event rows. Use `--output json` when you need the full event document and context, keeping the same explicit range/source bounds. Output size depends on the selected events; see [output formats](../reference/output-formats.md) rather than relying on a fixed size ratio.

### Examples

Read a small interval from the event log:

```bash
cratis chronicle events get --from 0 --to 100
```

Get events from sequence number 100 to 200:

```bash
cratis chronicle events get --from 100 --to 200
```

Read a bounded interval for a specific event source:

```bash
cratis chronicle events get --event-source-id "user-42" --from 100 --to 200
```

Filter to a single event type:

```bash
cratis chronicle events get --event-type UserRegistered --from 100 --to 200
```

Filter to a specific generation of an event type:

```bash
cratis chronicle events get --event-type UserRegistered+2 --from 100 --to 200
```

Filter to multiple event types:

```bash
cratis chronicle events get --event-type UserRegistered,UserEmailChanged --from 100 --to 200
```

Combine filters for targeted inspection:

```bash
cratis chronicle events get --event-source-id "user-42" --event-type UserRegistered --from 100 --to 200 --output json
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
