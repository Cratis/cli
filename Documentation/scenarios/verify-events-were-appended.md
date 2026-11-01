---
title: Verify events were appended
description: Inspect a bounded sequence interval for one expected event source and type before drawing conclusions about an application action.
---

You performed an action and want to know whether its event reached Chronicle.
A changed log tail is a useful boundary, but does not identify the cause of that
change. Read the expected event in a small interval, then investigate processing
separately.

## Before you begin

Complete [first inspection](../getting-started/index.mdx). Obtain an authorized
server, event store, namespace, expected event source ID, and event type/generation
from the application owner. Agree on the action to test; an append-producing
business action is a separate state-changing operation, not part of CLI inspection.

Examples assume `chronicle://localhost:35000`, store `myapp`, namespace `Default`,
sequence `event-log`, and source `user-42`. Substitute the actual scope and source
on every command. Event payloads may contain sensitive data; don't send them to
shared logs or assistant prompts.

## 1. Capture a boundary

Read the tail before the agreed action:

```bash
cratis chronicle events tail --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Record `tailSequenceNumber`. A successful empty sequence differs from a failed
query: a non-zero/error response means the boundary is unavailable. Stop and fix
the inspection problem rather than assuming no events exist.

Have the authorized user perform the intended business action, then read the
tail once more with the same command. If it advanced, that only shows something
was appended in this scope. It does not prove which action caused the append.
If it didn't advance, recheck scope and the application's result before concluding
that its append failed.

## 2. Read only the relevant interval

Choose a small explicit sequence range near those observations. Below, `100` to
`150` is illustrative: replace it with the actual interval, and split a large
interval only within an agreed request budget. Don't default to reading the log
from zero.

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --from 100 --to 150 --output plain
```

Inspect the event source, event type/generation, sequence number, and occurrence
metadata. Request JSON for that same bounded range only if payload/context detail
is necessary. To narrow by type, use the actual identifier and generation:

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --event-type UserRegistered+1 --from 100 --to 150 --output json
```

`UserRegistered+1` is an example; omitting `+generation` defaults to generation 1.
An empty successful filtered result means no matching events in that interval,
not that no event was ever appended. Check the source, generation, interval, and
scope with the owner. A request failure is unknown, not empty.

Stop after the chosen interval/request budget. Don't scan every source, remove
all bounds, or keep polling until a match appears. If you cannot identify the
boundary, report the missing evidence.

## 3. Separate append from processing

Finding the expected source/type/generation in the interval establishes a matching
append in that scope. Correlate with application evidence before attributing it
to one action, especially when other writers are active. It does not prove that
an observer handled it or a read model changed.

For a stale view, continue with [Find a missing projection](find-a-missing-projection.md)
or [the stuck-observer investigation](fix-a-stuck-observer.md). A subscribed
observer's position need not equal the global tail. Corroborate its relevant
progress, failures, and resulting model/key instead. No event read authorizes
retry, replay, quarantine clearing, or recommendation execution.

The [events reference](../chronicle/events.md) covers other sequences, output
formats, and range/type filters.
