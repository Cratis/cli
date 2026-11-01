---
title: Find and fix a stuck observer
description: Inspect one observer and partition, distinguish failure from unknown state, and approve only the recovery action the evidence supports.
---

A view stopped updating. Before retrying or replaying, identify the observer,
read its failure, and fix the underlying cause. Recovery can run handlers again
and repeat effects outside Chronicle.

## Before you begin

Complete [first inspection](../getting-started/index.mdx) and obtain permission
to inspect the target server, event store, namespace, and relevant application
data. If you don't know the linked observer, use
[Find a missing projection](find-a-missing-projection.md) first.

Examples use `chronicle://localhost:35000`, `myapp`, and `Default`. Replace all
three with the authorized scope. Replace `my-observer-id` and `user-42` with the
exact returned observer/partition IDs. A partition is often an event source ID,
not always. Start with one sweep and one failure inspection; agree before
expanding scope or polling.

## 1. Gather evidence without recovery

```bash
cratis chronicle diagnose --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Check `checksComplete` and `checksCouldNotRun` before interpreting health. A
complete empty result is different from an unavailable check. If a needed check
failed, report unknown state and resolve access/connectivity before recovery.

Inspect the known observer and its failures:

```bash
cratis chronicle observers show my-observer-id --server chronicle://localhost:35000 -e myapp -n Default -o json
cratis chronicle failed-partitions list --server chronicle://localhost:35000 -e myapp -n Default --output plain
cratis chronicle failed-partitions show my-observer-id user-42 --server chronicle://localhost:35000 -e myapp -n Default -o json
```

The failure detail includes the error/stack trace and failing sequence position.
Keep sensitive payloads, stack traces, and credentials out of shared logs. If no
failure is returned, don't manufacture one from lag: check expected event types,
generation, namespace, and model state. An observer's subscribed position need
not equal the global tail. A disconnected observer may instead need its owning
application restored.

## 2. Fix the cause and decide whether state is valid

Use the error to distinguish an application bug, bad input, and a transient
dependency outage. The application owner must decide and apply any fix outside
this CLI procedure. Do not repeat recovery while the same cause remains.

If prior accumulated state remains valid, retry is usually preferable to
rebuilding. If that state is no longer trustworthy, discuss a partition replay
or [observer replay](replay-a-projection.md). These are alternative decisions,
not an escalation script to run automatically.

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

If the owner separately approves clearing the quarantine flag, use
`observers clear-partition-quarantine` for this same exact target. That command
also prompts for confirmation; it does not fix the cause. Decide whether a retry
is still appropriate afterward. For an approved partition rebuild,
`observers replay-partition` deletes that partition's state and prompts before
rebuilding it. See the [observer command reference](../chronicle/observers.md)
for exact syntax and flags. Whole-observer replay has a larger blast radius.

## 4. Verify once, then stop or report

A success message means the action started, not that recovery finished. Make one
fresh observer/failure inspection and check the relevant model/key, using the
same explicit target. If processing is still ongoing, agree on a bounded later
check rather than entering an unbounded watch loop.

Success means the targeted failure is resolved and the expected relevant state
is observed—not merely a zero CLI exit code or global-tail equality. If the
failure persists, stop, preserve sanitized evidence, and return to the owner.
Don't widen recovery or repeat handlers indefinitely.
