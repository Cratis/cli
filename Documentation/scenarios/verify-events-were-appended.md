---
title: Verify events were appended
description: Inspect recent event output for one expected source and type, understand retrieval limits, and verify an append separately from processing.
---

You performed an action and want to know whether its event reached Chronicle.
Compare the log tail before and after the action, then look for the expected
event near those positions. Other writers can advance the tail too, so the
changed number alone cannot identify your event. Whether an observer processed
it is a separate check.

## Before you begin

Complete [first inspection](../getting-started/index.mdx). Ask the application
owner for the server, event store, namespace, event source ID, and event
type/generation you should inspect. Agree on who may perform the business action
and what to test: producing an event changes state, while the CLI commands below
only inspect it.

Examples assume `chronicle://localhost:35000`, store `myapp`, namespace `Default`,
sequence `event-log`, and source `user-42`. Substitute the actual scope and source
on every command. Event payloads may contain sensitive data; don't send them to
shared logs or assistant prompts.

## 1. Capture a boundary

Read the tail before the agreed action:

```bash
cratis chronicle events tail --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Record `tailSequenceNumber`. A successful response can report an empty sequence.
A nonzero exit or error response means you could not read the boundary; resolve
that inspection problem before continuing, rather than assuming no events exist.

Have the authorized user perform the intended business action, then read the
tail once more with the same command. If it advanced, that only shows something
was appended in this scope. It does not prove which action caused the append.
If it didn't advance, recheck scope and the application's result before concluding
that its append failed.

## 2. Inspect a recent output interval

Choose a recent `--from` near the tail you just recorded. The example positions
`100` and `150` stand for the interval around your action; replace both before
running the command, along with the target and event source.

`--to` trims output **after** retrieval. Even a small output interval can retrieve
all remaining matching events from `--from` onward. Source/type filters narrow
the matches, but neither they nor splitting an old interval into requests impose
a hard network or server-work limit. If your inspection requires that limit,
stop: this command has no count/limit option to enforce it. Do not start at zero
or search old history to find a recent action.

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --from 100 --to 150 --output plain
```

Match the returned event source, type identifier, sequence number, and occurrence
metadata to the expected event. If you need its generation or payload/context,
request JSON for the same approved recent interval. The type-filtered example
uses `UserRegistered+1`; replace it with the actual type:

```bash
cratis chronicle events get --server chronicle://localhost:35000 -e myapp -n Default --event-source-id user-42 --event-type UserRegistered+1 --from 100 --to 150 --output json
```

The command accepts `+1` but sends only the type identifier, so the suffix does
not filter generation. Check `generation` in the returned JSON to confirm the
version you expected.

A successful empty result means no events matched that output interval and
filter. It does not prove that no event was ever appended. Recheck the source,
type, interval, and target with the owner. If the request failed, resolve that
failure before treating the result as empty.

Stop after the agreed number of requests. That number cannot cap the work of
an individual retrieval. If you lack a recent boundary or cannot find the event,
report what is missing rather than scanning every source, searching backward
through history, or polling until something matches.

## 3. Separate append from processing

Finding the expected source/type/generation in the interval establishes a matching
append in that scope. Correlate with application evidence before attributing it
to one action, especially when other writers are active. It does not prove that
an observer handled it or a read model changed.

If the event is present but the view is stale, follow
[Find a missing projection](find-a-missing-projection.md) to identify the linked
observer and model. For a known failed observer, use
[the stuck-observer investigation](fix-a-stuck-observer.md) to read its failure
and choose a recovery with the owner. Check relevant progress and the resulting
model/key; an observer with filtered subscriptions need not equal the global tail.
Reading an event does not authorize retry, replay, quarantine clearing, or
recommendation execution.

The [events reference](../chronicle/events.md) covers other sequences, output
formats, and range/type filters.
