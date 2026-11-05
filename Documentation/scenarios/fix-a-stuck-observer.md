---
title: Fix a stuck Chronicle observer with the Cratis CLI
description: Use the Cratis CLI to inspect a failed Chronicle observer partition, fix the cause, and choose between retrying processing and rebuilding state.
---

<a id="find-and-fix-a-stuck-observer"></a>

A stale read model or a missing side effect can mean that a Cratis Chronicle observer failed while
processing an event. An observer is a projection, reducer or reactor that handles events; a failed
partition stops progress for the affected key while other partitions can continue.

Use the Cratis CLI to read the failure before changing state. Confirm the server, event store and
namespace in your [context](../context/index.md), then follow the steps below. If you have not checked
whether the expected event was written, [verify the events first](./verify-events-were-appended.md).

## 1. Spot the stuck observer

Start with the health check to see whether it reports failed partitions:

```bash
cratis chronicle diagnose
```

If it reports failed partitions, list the observers and look at their state and sequence position:

```bash
cratis chronicle observers list --output plain
```

Look for an observer whose sequence number is behind the event log tail or whose state is not active.
Use the failure details in the next step to establish why it stopped.

## 2. Read the failure

List the failed partitions to find the affected partition key and observer:

```bash
cratis chronicle failed-partitions list
```

Read the failing sequence number, exception and attempt history:

```bash
cratis chronicle failed-partitions show <OBSERVER_ID> <PARTITION> --detailed
```

Use the partition key returned by `list` for `<PARTITION>`. It is typically an event source id, such as
`user-42`. The `--detailed` flag shows the full stack trace; add `-o json` for structured error analysis.

## 3. Fix the cause, then retry

Use the exception and attempt history to fix the cause. If the fix changes your application code,
redeploy it before retrying. Then resume the failed partition without discarding its accumulated state:

```bash
cratis chronicle observers retry-partition <OBSERVER_ID> <PARTITION>
```

Retry re-processes the event that failed, letting the observer pick up where it stopped. Use it when
you have resolved a transient error or corrected the handler without needing to rebuild existing data.
The command reports when a retry is refused; a successful request starts recovery, so check progress
afterwards.

If the retry is refused because the partition is quarantined, it has exhausted its retry attempts.
Release the partition quarantine before it can retry. `clear-quarantine` only clears the observer,
not its partitions:

```bash
cratis chronicle observers clear-partition-quarantine <OBSERVER_ID> <PARTITION>
```

This resets the retry budget and starts the retry (`--no-retry` only clears). It needs Chronicle 19.29.0
or later. The retry runs the handler again, so account for side effects it may already have performed.

## 4. If the state is corrupt, replay instead

Retry does not repair data the handler already wrote. If a bug left the partition's read-model state
incorrect before failing, rebuild that partition from sequence zero:

```bash
cratis chronicle observers replay-partition <OBSERVER_ID> <PARTITION>
```

This discards the partition's accumulated state and rebuilds it from history. Other partitions are
untouched.

## Done when

Run the health check again:

```bash
cratis chronicle diagnose
```

Check that it reports no failed partitions and that the observer has caught up. For a read model,
inspect the affected data too; for a reactor, check the expected side effect. If a projection change
requires rebuilding the whole read model, continue with [Replay a projection](./replay-a-projection.md).
