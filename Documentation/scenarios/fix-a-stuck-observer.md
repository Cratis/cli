---
title: Find and fix a stuck observer
description: Inspect one observer and partition, distinguish failure from unknown state, and approve only the recovery action the evidence supports.
---

A view stopped updating. Find the observer that builds it and read the recorded
failure before choosing a recovery. Fixing the cause matters: a retry can run the
same handler again, including effects outside Chronicle.

## Before you begin

Complete [first inspection](../getting-started/index.mdx) and obtain permission
to inspect the target server, event store, namespace, and relevant application
data. If you don't know the linked observer, use
[Find a missing projection](find-a-missing-projection.md) first.

Start with one diagnostic sweep and one failure inspection. The examples use
`chronicle://localhost:35000`, store `myapp`, and namespace `Default`; replace all
three with the values you are authorized to inspect. Use the exact returned
observer ID and partition key for `my-observer-id` and `user-42`. A partition is
often an event source ID, but not always. Agree before inspecting more targets
or adding repeated checks.

## 1. Gather evidence without recovery

```bash
cratis chronicle diagnose --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Read `checksComplete` and `checksCouldNotRun` first. If a needed check could not
run, a zero failure count does not tell you that the observer is healthy. Resolve
the reported access or connectivity problem before choosing recovery. Even a
complete healthy report does not prove that this view is current.

Use the observer ID to inspect its state, then match the partition key returned
in the failure list to the one you inspect with `failed-partitions show`:

```bash
cratis chronicle observers show my-observer-id --server chronicle://localhost:35000 -e myapp -n Default -o json
cratis chronicle failed-partitions list --server chronicle://localhost:35000 -e myapp -n Default --output plain
cratis chronicle failed-partitions show my-observer-id user-42 --server chronicle://localhost:35000 -e myapp -n Default -o json
```

`failed-partitions show` gives you the error, stack trace, and failing sequence
position. Use those details to locate the event and code that failed, but keep
sensitive payloads, stack traces, and credentials out of shared logs.

If the list succeeds with no failure, check the expected event types, generation,
namespace, and [model state](find-a-missing-projection.md) instead of inferring a
failure from lag. A request error means the inspection could not run. The observer
may subscribe to only some events, so its position need not equal the global tail.
If it is disconnected, its owning application may need restoring.

## 2. Fix the cause and decide whether state is valid

The error should help the application owner distinguish a code bug, bad input,
and an unavailable dependency. Have the owner apply the appropriate fix before
running recovery; repeating the handler cannot repair its code or input.

Then decide whether the accumulated state is still valid. If it is, retry can
continue processing without rebuilding it. If the state itself is wrong, discuss
a partition replay or [observer replay](replay-a-projection.md). Choose the action
that addresses the problem; a refused retry is not a reason to work through
progressively broader recovery commands.

## 3. Approve one exact action

:::caution[Recovery changes state]
Agree on the server, event store, namespace, observer, partition, action, and
possible repeated external effects with the responsible owner. Diagnostic
recommendations, catalog effect metadata, and a confirmation flag do not grant
permission. Don't add `--yes` merely to make automation continue.
:::

After fixing the cause, an approved retry resumes the failed partition without
resetting accumulated state:

```bash
cratis chronicle observers retry-partition my-observer-id user-42 --server chronicle://localhost:35000 -e myapp -n Default
```

Retry prompts for confirmation. It refuses quarantined observers/partitions and
partitions that are not failed. Stop on refusal and reassess; do not automatically
clear quarantine or substitute replay.

If the owner approves only clearing the partition quarantine, explicitly prevent
this command from starting a retry:

```bash
cratis chronicle observers clear-partition-quarantine my-observer-id user-42 --no-retry --server chronicle://localhost:35000 -e myapp -n Default
```

The command prompts, clears the quarantine, and resets the retry budget. With
`--no-retry`, it leaves the partition ready for a separately approved retry; it
does not repair the cause. Without that flag, clearing also starts a retry from
the failed sequence number. Approval must then cover both actions and any
repeated handler effects. After clearing only, inspect the partition once before
agreeing on its next action.

If the partition needs rebuilding instead, `observers replay-partition` prompts
before deleting and rebuilding its state. The [observer command reference](../chronicle/observers.md)
has the exact syntax. Whole-observer replay rebuilds more state and needs its
own justification.

## 4. Verify once, then stop or report

After retry or replay starts, run `observers show` and `failed-partitions list`
once more for the same target. Check whether the targeted failure is resolved,
then inspect the expected model/key through the
[read-model commands](../chronicle/read-models.md), or in the owning application
for client-owned state. A successful clear-only command confirms neither retry
nor finished recovery.

The expected state and resolved failure are the completion check; a zero exit
code or global-tail match is not enough. If processing is still running, agree
on a bounded later check. If the failure remains or inspection fails, stop and
return the sanitized details to the owner instead of repeating handlers or
widening recovery.
