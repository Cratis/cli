---
title: Replay a projection
description: Rebuild one projection's state only after confirming its observer, target scope, and replay effects with the responsible owner.
---

A projection mapping changed and existing state may need rebuilding. Replay
re-runs event handling; it isn't a way to discover a missing projection or fix an
unregistered definition. First use [Find a missing projection](find-a-missing-projection.md)
to establish the definition, runtime observer, and read-model ownership.

## Before approving replay

- Complete [first inspection](../getting-started/index.mdx) and confirm the authorized server, event store, and namespace.
- Link the read-model definition's `observerIdentifier` to a returned runtime observer. Do not assume a projection ID is the observer ID.
- Determine with the owner whether existing state is invalid. If the cause is fixed and state is valid, prefer an approved [partition retry](fix-a-stuck-observer.md).
- Consider handler side effects. Replaying a reactor can resend emails, repeat writes, or repeat external API calls. Do not include unrelated reactors in a projection rebuild.

Examples use `chronicle://localhost:35000`, store `myapp`, namespace `Default`,
and a confirmed observer `my-projection-id`. Replace all with the approved values.

## 1. Inspect the target before changing it

```bash
cratis chronicle observers show my-projection-id --server chronicle://localhost:35000 -e myapp -n Default -o json
```

Check observer type and state, relevant positions, failures, and linked model.
If it is missing or the request is unavailable, stop and resolve that evidence
gap. An empty/unknown target is not permission to replay another observer.

:::caution[Obtain approval for the blast radius]
Replay deletes and rebuilds the target's accumulated state from the beginning.
It can be expensive and can repeat handler effects. Agree on the exact scope,
expected disruption, and follow-up checks with its responsible owner first.
A catalog effect or diagnostic recommendation is not permission.
:::

If only one partition requires rebuilding, prefer the approved narrower action:

```bash
cratis chronicle observers replay-partition my-projection-id user-42 --server chronicle://localhost:35000 -e myapp -n Default
```

Replace `user-42` with the confirmed partition ID. This command prompts before
deleting that partition's state; other partitions remain intact. Do not use it
as an automatic workaround for a refused retry.

## 2. Start only the approved replay

For a whole-observer rebuild, the command is:

```bash
cratis chronicle observers replay my-projection-id --server chronicle://localhost:35000 -e myapp -n Default
```

Unlike partition replay, **whole-observer replay starts without an interactive
confirmation prompt**. Review the exact command and obtain approval before
execution. This is not a command to paste merely to inspect its behavior.
`--yes` on prompting commands skips a prompt; it is not evidence of permission.

The success response says replay started. It does not report completion.

## 3. Check relevant progress and resulting state

Perform one fresh `observers show` with the same explicit target and inspect the
relevant model/key. For client-owned state, verify in the owning application,
not Chronicle's instance explorer. See the [read-model reference](../chronicle/read-models.md).

The observer may report `Replaying` while rebuilding, then return to `Active`.
Do not require its last handled position to equal the global tail: subscriptions
may cover only some event types. Corroborate the expected relevant events, absence
of targeted failures, and resulting model state.

Agree on a later check or a fixed time/refresh budget if it is still running.
Stop and report repeated failures or unavailable checks rather than replaying
again, polling without a limit, or broadening to other observers. Recovery is
complete only when the owner has the expected evidence, not when the start
command exits successfully.
