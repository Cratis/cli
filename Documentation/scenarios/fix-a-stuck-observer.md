# Find and fix a stuck observer

A read model looks stale, or a reactor's side effect never fired. In an event-sourced system that
usually means an **observer** — a projection, reducer, or reactor — hit an event it couldn't process and
**paused that partition**. The rest of the system keeps running; only the failing event source is
stuck. Here's how to find it and get it moving again.

## 1. Spot the stuck observer

Start with the health check — it counts failed partitions across the whole store in one shot:

```bash
cratis chronicle diagnose
```

If it reports failed partitions, list the observers and look at their state and sequence position:

```bash
cratis chronicle observers list --output plain
```

An observer whose sequence number is behind the event log tail, or whose state isn't active, is your
suspect.

## 2. Read the failure

List the failed partitions to see *which* event source failed and on which observer:

```bash
cratis chronicle failed-partitions list
```

Then read the actual error — the failing sequence number, the exception, and the attempt history:

```bash
cratis chronicle failed-partitions show <OBSERVER_ID> <PARTITION> --detailed
```

`<PARTITION>` is the event source id (for example `user-42`). The `--detailed` flag gives you the full
stack trace; add `-o json` if you want to pipe it somewhere.

## 3. Fix the cause, then retry

The failure is almost always a bug in the observer's code — a null it didn't expect, a missing case.
Fix that in your application and redeploy. Then resume the partition **without** losing its state:

```bash
cratis chronicle observers retry-partition <OBSERVER_ID> <PARTITION>
```

Retry re-processes the event that failed. For a transient error or a corrected bug, this is the right
recovery path — the observer picks up where it stopped.

## 4. If the state is corrupt, replay instead

If the partition's read-model state is wrong (not just stuck) — say a bug wrote bad data before it
threw — retrying won't fix what's already there. Rebuild just that partition from sequence zero:

```bash
cratis chronicle observers replay-partition <OBSERVER_ID> <PARTITION>
```

This discards the partition's accumulated state and rebuilds it from history. Other partitions are
untouched.

## Done when

```bash
cratis chronicle diagnose
```

reports no failed partitions and the observer's sequence number has caught up to the tail. If a whole
observer is broken (a schema change, not one bad partition), see [Replay a projection](./replay-a-projection.md)
for a full rebuild.
