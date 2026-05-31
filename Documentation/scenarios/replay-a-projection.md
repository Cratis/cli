# Replay a projection

You changed how a read model is built — added a field, fixed a mapping, reshaped the projection — and
now the existing read model is out of date. Because the events are still the source of truth, you don't
migrate the read model: you **replay** the observer and let it rebuild from history.

:::caution
Replay is destructive to the read model: the observer discards all accumulated state and rebuilds it
from sequence zero. The event log is untouched — that's the whole point — but the read model is
unavailable or incomplete while it catches up. Prefer [`replay-partition`](./fix-a-stuck-observer.md) when
only one event source is affected.
:::

## 1. Find the observer

Projections, reducers, and reactors are all observers. List them and grab the id of the one whose read
model you changed:

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

The command asks for confirmation first — replay re-processes every event the observer subscribes to. In
an automated pipeline, skip the prompt:

```bash
cratis chronicle observers replay <OBSERVER_ID> --yes
```

## 3. Watch it catch up

Replay runs in the background. Watch the health view refresh until the observer reaches the tail:

```bash
cratis chronicle diagnose --watch
```

Or check the observer's sequence position directly:

```bash
cratis chronicle observers show <OBSERVER_ID>
```

## Done when

The observer's sequence number matches the event log tail and its read model reflects the new shape.
Spot-check the rebuilt data with [`read-models`](/cli/chronicle/read-models/) or by
[reading the events](./verify-events-were-appended.md) it was built from.
