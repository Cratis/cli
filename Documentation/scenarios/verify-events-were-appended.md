---
title: Verify events were appended
description: Inspect recent event output for one expected source and type, understand retrieval limits, and verify an append separately from processing.
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

## 2. Inspect a recent output interval

`--to` is an output filter, applied locally after the CLI retrieves events from
`--from` onward. It is not a hard network or server-work budget: even a small
output interval can retrieve all remaining matching events in the log. Splitting
an old interval into requests does not fix this retrieval risk.

Choose an approved `--from` near the recently observed tail, and a small output
interval around the agreed action. Below, `100` to `150` is illustrative: replace
both with those recent positions before running. Do not read old intervals or
start at zero to search full history. Source/type filters narrow matches but do
not enforce a hard retrieval budget. If that budget is required, stop and report
that this command has no count/limit option to enforce it.

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --from 100 --to 150 --output plain
```

Inspect the event source, event type identifier, sequence number, and occurrence
metadata. Request JSON for the same approved recent start and output interval
only if generation or payload/context detail is necessary. To narrow by type,
use the actual identifier:

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --event-type UserRegistered+1 --from 100 --to 150 --output json
```

`UserRegistered+1` is an example. The command accepts the suffix but sends only
type identifiers, so it does not filter by generation. Check `generation` in the
returned JSON rather than assuming the suffix guarantees a match. An empty
successful filtered result means no matching events in that output interval,
not that no event was ever appended. Check the source, type identifier, interval,
and scope with the owner. A request failure is unknown, not empty.

Stop after the approved request count; that count does not cap the work of an
individual request. Don't scan every source, move `--from` back through history,
or keep polling until a match appears. If you cannot identify a recent boundary,
report the missing evidence.

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
