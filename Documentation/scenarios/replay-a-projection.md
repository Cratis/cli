---
title: Replay a projection
description: Rebuild one projection's state only after confirming its observer, target scope, and replay effects with the responsible owner.
---

A changed projection mapping may leave the stored view out of date. Replay
rebuilds that state by handling the events again. It cannot register a missing
projection. If you have not yet linked the definition to its running observer
and read-model owner, start with [Find a missing projection](find-a-missing-projection.md).

## Before approving replay

Complete [first inspection](../getting-started/index.mdx) and confirm the server,
event store, and namespace you are authorized to change. Follow the read-model
definition's `observerIdentifier` to the returned runtime observer. The examples
use `my-projection-id` for that **observer ID**, despite the placeholder's name;
a projection ID is not necessarily an observer ID.

Decide with the application owner whether the accumulated state needs rebuilding.
If the cause is fixed and the existing state is valid, an approved
[partition retry](fix-a-stuck-observer.md) avoids rebuilding it. Check the handler's
external effects too: replaying a reactor can resend emails, repeat writes, or
repeat API calls. Leave unrelated reactors out of a projection rebuild.

Replace `chronicle://localhost:35000`, store `myapp`, namespace `Default`, and
`my-projection-id` in every example with the approved target and observer ID.

## 1. Inspect the target before changing it

```bash
cratis chronicle observers show my-projection-id --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Check the observer's type, state, relevant positions, failures, and linked model.
If a successful request cannot find it, recheck the ID and namespace. If the
request fails, resolve that inspection problem first. Neither result tells you
to replay a different observer.

:::caution[Obtain approval for the blast radius]
Replay deletes and rebuilds the target's accumulated state from the beginning.
It can be expensive and can repeat handler effects. Agree on the exact scope,
expected disruption, and follow-up checks with its responsible owner first.
A catalog effect or diagnostic recommendation is not permission.
:::

## 2. Start only the approved replay

If only one partition needs rebuilding, choose the narrower action with its owner:

```bash
cratis chronicle observers replay-partition my-projection-id user-42 --server chronicle://localhost:35000 -e myapp -n Default
```

Replace `user-42` with the returned partition key, often an event source ID. This
command prompts before deleting that partition's state; other partitions remain
intact. A refused retry is a reason to review the failure, not to substitute replay.

For an approved whole-observer rebuild, use:

```bash
cratis chronicle observers replay my-projection-id --server chronicle://localhost:35000 -e myapp -n Default
```

Whole-observer replay prompts for confirmation, as partition replay does. Review
the target and obtain approval before running either command. `--yes` skips that
confirmation; it does not grant permission to rebuild state or repeat effects.

The success response says replay started. Check the observer and view to find
out whether rebuilding has finished.

## 3. Check relevant progress and resulting state

Run `observers show` once more with the same explicit target, then inspect the
expected model/key using the [read-model commands](../chronicle/read-models.md).
For client-owned state, check the view in the application that stores it instead.
Job inspection is optional. If you already know the job ID for this operation,
use [jobs get](../chronicle/jobs.md#get). Replay does not return that ID. Otherwise,
continue with `observers show` and the model/key checks below; do not select a job
by type or creation time alone.

The observer may report `Replaying` while rebuilding, then return to `Active`.
Do not require its last handled position to equal the global tail: subscriptions
may cover only some event types. Corroborate the expected relevant events, absence
of targeted failures, and resulting model state.

If processing is still running, agree on when to check again or how many refreshes
to make. Repeated failures call for another look at the cause, not another replay
or a wider set of observers. Report checks that could not run. Recovery is complete
when the expected state is present and the targeted failures are resolved, not
when the command that starts replay exits successfully.
