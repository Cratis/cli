---
title: Find a missing projection
description: Trace a missing projection from event-store registration through namespace observer state to the read model's owner and stored data.
---

Your application expects a projected view, but the view is missing. Don't replay
yet: first establish whether the definition exists, whether its observer runs in
the namespace, and who owns the resulting state.

## Before you begin

Install the CLI and complete [first inspection](../getting-started/index.mdx).
Obtain an authorized Chronicle server, the application's event store/namespace,
and the expected projection/model identity from its owner. Choose a small request
budget (for example, the discovery requests below and one model lookup); don't
scan more environments until something matches.

Examples use `chronicle://localhost:35000`, store `myapp`, and namespace `Default`.
Replace them with the approved target on **every** command. Reads don't change
Chronicle state, though CLI defaults or first-run setup may affect local config.
Don't share credential-bearing connection strings or model/event payloads.

## 1. Confirm the scope

If the store and namespace aren't already confirmed:

```bash
cratis chronicle event-stores list --server chronicle://localhost:35000 --output plain
cratis chronicle namespaces list --server chronicle://localhost:35000 -e myapp --output plain
```

Use a returned store and namespace. An empty successful discovery is a result to
resolve with the application owner; an unavailable request is unknown state.
Stop either way if you cannot identify the intended scope.

## 2. Look for the declaration in the event store

```bash
cratis chronicle projections list --server chronicle://localhost:35000 -e myapp --output plain
```

Definitions belong to the **event store**, not a namespace. Adding `-n` will not
filter this list. If the expected definition is absent from a successful response,
check the owning application's registration and target store with its owner.
Replay cannot create an unregistered projection. Stop rather than inventing an
identifier or changing unrelated state.

Use a returned identifier to inspect exactly one declaration. Replace
`my-projection-id` below:

```bash
cratis chronicle projections show my-projection-id --server chronicle://localhost:35000 -e myapp -o json
```

The result wraps `identifier`, `containerName`, and `declaration`; the declaration
is a serialized string. Inspect its event mappings and target model. An unknown
ID is a not-found error, not a request to register it.

## 3. Identify the model and its owner

```bash
cratis chronicle read-models list --server chronicle://localhost:35000 -e myapp -o json
```

This is also a store-level definition request. Match the declared target to the
returned model and use its `identifier` for model queries, not its display name,
container, or projection ID. Record its `generation`, `observerIdentifier`,
`owner`, and `queryable` fields. If that link is unclear, ask the application
owner instead of guessing.

If `queryable` is false / `owner` is `Client`, **stop Chronicle instance retrieval**.
State belongs to the client application, so continue inspection there with its
owner. An unavailable server-side instance is not evidence of failed projection.

## 4. Inspect the observer in the namespace

```bash
cratis chronicle observers list --server chronicle://localhost:35000 -e myapp -n Default --output plain
```

Match the linked `observerIdentifier`. Replace `my-observer-id` with that exact ID:

```bash
cratis chronicle observers show my-observer-id --server chronicle://localhost:35000 -e myapp -n Default -o json
```

A declaration can exist while no matching runtime observer is returned in this
namespace. Recheck the namespace and the application's runtime registration; do
not replay a guessed observer. A failed request means inspection is unavailable,
not that the observer is absent. For failures or quarantine on a known observer,
continue with [the stuck-observer investigation](fix-a-stuck-observer.md).

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

Stop after the chosen lookup. The page wraps `totalCount`, `page`, `pageSize`, and
`instances`. A successful empty page differs from a query failure. If state is
missing or stale, verify the [expected append](verify-events-were-appended.md),
then the linked observer's relevant progress/failures; global-tail equality is
not required for an observer with filtered subscriptions.

You've succeeded when you can identify the break—registration, namespace
runtime, ownership, missing input, or model state—or report precisely which
inspection couldn't run. None of those findings alone authorizes retry, replay,
quarantine changes, or recommendation execution. Preserve the target IDs and
minimal sanitized evidence for the owner; agree on a separate recovery action.
