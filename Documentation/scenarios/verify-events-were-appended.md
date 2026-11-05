---
title: Verify Chronicle events with the Cratis CLI
description: Read the Chronicle event log with the Cratis CLI to check what a command appended, inspect event content, and decide where to investigate next.
---

<a id="verify-events-were-appended"></a>

When a command appears to succeed but a read model has not changed, check the Cratis Chronicle event
log first. The Cratis CLI can read the appended events directly, so you can separate what the command
wrote from what an observer has processed into a read model.

Before checking, confirm the server, event store and namespace in your [context](../context/index.md).
Use the same target throughout; a query against a different namespace cannot tell you what this
command wrote.

## 1. Check the tail moved

The tail is the sequence number of the last appended event. Compare it before and after the action
to see whether the log advanced:

```bash
cratis chronicle events tail --output plain
```

For a specific event source — say the account you just acted on:

```bash
cratis chronicle events tail --event-source-id "account-42"
```

## 2. Read the events

A moving tail tells you that something was appended, but not whether it was the event you expected.
List the events for the event source you acted on:

```bash
cratis chronicle events get --event-source-id "account-42"
```

Filter to the event type you expected, and get the full document as JSON to inspect its content:

```bash
cratis chronicle events get --event-source-id "account-42" --event-type AccountOpened --output json
```

Check the event type and content, not just whether the list has a row. If the expected event is absent,
check your query's target and filters, then investigate the command's write path for a validation
failure, constraint or unhandled case. If the event is present but the read model is wrong, follow
[Find and fix a stuck observer](./fix-a-stuck-observer.md) to inspect processing failures.

## 3. Audit a range

To inspect more than one event source, read a sequence range and export it:

```bash
cratis chronicle events get --from 0 --to 500 --output json > audit.json
```

The export covers sequence numbers `0` through `500`; it does not include later events. Use
`--output plain` when scanning rows in the terminal. Keep JSON when you need to inspect event content.

## Done when

You have found the expected events and checked their content, or confirmed they are absent from the
target and filters you checked. If the events are present and the projection definition has changed,
[replay the projection](./replay-a-projection.md) to rebuild its read model from history.
