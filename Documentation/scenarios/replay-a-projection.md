---
title: Replay a Chronicle projection with the Cratis CLI
description: Rebuild a Chronicle projection's read model from event history with the Cratis CLI, understand what replay discards, and check its progress.
---

<a id="replay-a-projection"></a>

After you change a Cratis Chronicle projection, existing read-model data may still reflect the old
mapping. Replay uses the event history to rebuild that data with the updated projection. Use the
Cratis CLI to start the replay, then watch the observer's progress before relying on the rebuilt data.

Confirm the target server, event store and namespace in your [context](../context/index.md) before
starting. If only one partition has failed, [retry that partition](./fix-a-stuck-observer.md) after
fixing the cause; a full replay is not needed just to resume processing.

:::caution
Replay discards the observer's accumulated read-model state and rebuilds it from sequence zero. The
event log is untouched, but the read model is unavailable or incomplete while it catches up. If state
needs rebuilding for only one event source, use [`replay-partition`](./fix-a-stuck-observer.md).
:::

## 1. Find the observer

An observer processes events as they arrive; projections, reducers and reactors are observer types.
List the projections and copy the id of the one whose read model you changed:

```bash
cratis chronicle observers list --type projection
```

Use `-q` to get just the ids if you want to script it:

```bash
cratis chronicle observers list --type projection -q
```

## 2. Replay it

```bash
cratis chronicle observers replay <OBSERVER_ID>
```

The command asks for confirmation because replay re-processes every event the observer subscribes
to. Once you have checked the target, an automated pipeline can pass `--yes` to skip the prompt:

```bash
cratis chronicle observers replay <OBSERVER_ID> --yes
```

## 3. Watch it catch up

A successful replay command means the replay has started, not that the read model is ready. Replay
runs in the background. Watch the health view refresh while the observer catches up:

```bash
cratis chronicle diagnose --watch
```

Or check the observer's sequence position directly:

```bash
cratis chronicle observers show <OBSERVER_ID>
```

## Done when

The observer's sequence number matches the event log tail and its read model reflects the new shape.
Spot-check the rebuilt data with [`read-models`](../chronicle/read-models.md) or by
[reading the events](./verify-events-were-appended.md) it was built from.
