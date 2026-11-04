---
title: Find a missing projection
description: Trace a missing projection from event-store registration through namespace observer state to the read model's owner and stored data.
---

Your application expects a projected view, but you cannot find it. Start by
finding its definition, then follow the linked observer to the application or
server that stores the view. Replay only becomes useful if those pieces exist
and the stored state needs rebuilding.

## Before you begin

Install the CLI and complete [first inspection](../getting-started/index.mdx).
Ask the application owner for the projection/model identity and the Chronicle
server, event store, and namespace you are authorized to inspect. Work through
the discovery requests below and choose one model lookup. Agree with the owner
before adding requests or looking in another environment.

The examples use `chronicle://localhost:35000`, store `myapp`, and namespace
`Default`. Replace all three with your target on every command. These reads do
not change Chronicle state, though first-run setup or CLI defaults may affect
local configuration. Keep credentials and model/event payloads out of shared logs.

## 1. Confirm the scope

If the store and namespace aren't already confirmed:

```bash
cratis chronicle event-stores list --server chronicle://localhost:35000 --output plain
cratis chronicle namespaces list --server chronicle://localhost:35000 -e myapp --output plain
```

Use the returned store and namespace names. If discovery succeeds but the expected
name is missing, check the application's target with its owner. If the request
fails, resolve access or connectivity first; you do not yet know what exists.
Continue only when you can identify the intended store and namespace.

## 2. Look for the declaration in the event store

```bash
cratis chronicle projections list --server chronicle://localhost:35000 -e myapp --output plain
```

Projection definitions belong to the **event store**. Adding `-n` will not narrow
this list to a namespace. If a successful response lacks the expected definition,
ask the application owner to check its registration and target store. Replay
cannot create that missing registration, so resolve it before continuing.

Use a returned identifier to inspect exactly one declaration. Replace
`my-projection-id` below:

```bash
cratis chronicle projections show my-projection-id --server chronicle://localhost:35000 -e myapp -o json
```

The result contains `identifier`, `containerName`, and `declaration`. Read the
serialized `declaration` string to find the event mappings and target model;
you will use that model in the next step. An unknown ID returns not-found without
registering anything.

## 3. Identify the model and its owner

```bash
cratis chronicle read-models list --server chronicle://localhost:35000 -e myapp -o json
```

`read-models list` returns the definitions for the event store. Find the model
targeted by the projection and use its `identifier` when you query its data.
The display name, container name, and projection ID identify different things.
Keep its `generation` and follow `observerIdentifier` to the observer in the next
step. If you cannot establish that link, ask the application owner before continuing.

Check `owner` and `queryable` before reading instances. When `owner` is `Client`
and `queryable` is false, the application stores the view. Continue there with
its owner; Chronicle cannot return that view's instances. A failed server-side
lookup does not show that the projection failed.

## 4. Inspect the observer in the namespace

```bash
cratis chronicle observers list --server chronicle://localhost:35000 -e myapp -n Default --output plain
```

Match the linked `observerIdentifier`. Replace `my-observer-id` with that exact ID:

```bash
cratis chronicle observers show my-observer-id --server chronicle://localhost:35000 -e myapp -n Default -o json
```

A declaration can exist even when its observer is not running in this namespace.
If a successful request returns no matching observer, recheck the namespace and
the application's runtime registration. If the request fails, fix the inspection
problem before deciding whether the observer is absent. For failures or quarantine
on the confirmed observer, follow [the stuck-observer investigation](fix-a-stuck-observer.md).

## 5. Inspect only the needed model data

For a server-queryable model, substitute the confirmed model identifier and one
known key. The key must come from its mapping, not merely resemble a user ID:

```bash
cratis chronicle read-models get my-model-id user-42 --server chronicle://localhost:35000 -e myapp -n Default -o json
```

If you don't know a key, request one small page instead:

```bash
cratis chronicle read-models instances my-model-id --page 0 --page-size 5 --server chronicle://localhost:35000 -e myapp -n Default -o json
```

The instances response contains `totalCount`, `page`, `pageSize`, and `instances`.
A successful empty page found no instances. For a known-key `read-models get`,
`NotFound` means the lookup ran but no instance exists for that key. Neither result
means inspection was unavailable. If the query itself could not run, resolve that
failure before drawing conclusions about the model. Stop after the one chosen lookup.

For an empty page, a known-key `NotFound`, or a returned view that is stale, check the
[expected append](verify-events-were-appended.md), then the linked observer's
relevant progress and failures. An observer with filtered subscriptions need
not reach the global tail to have handled the events that matter to this view.

You should now know where to continue: registration, the namespace's runtime,
the application that owns the view, missing input, or model state. If an inspection
could not run, report that limitation with the target IDs and only the sanitized
details the owner needs. Agree separately on any recovery; finding the break
does not itself authorize retry, replay, quarantine changes, or recommendation
execution.
