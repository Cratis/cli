---
title: Projections
description: List projection definitions in a Chronicle event store and inspect one declaration without confusing it with namespace runtime state.
---

A projection is a declarative read-model builder driven by events. It defines how
event data maps onto a read model: fields, collections, creation/deletion, and
subscriptions. Its declaration is configuration, not the processing position of a
running observer or proof that a stored model instance exists.

If an expected projection is missing, follow [Find a missing projection](../scenarios/find-a-missing-projection.md)
to connect registration, runtime scope, and read-model ownership. Don't start with replay.

## list

Lists projection definitions registered in the specified **event store**. The
request does not filter by namespace, even though the shared `-n` option is accepted.

```bash
cratis chronicle projections list -e myapp --output plain
```

### Options

| Flag | Description |
| --- | --- |
| `-e, --event-store <NAME>` | Event store to inspect. Resolves from the context, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Accepted shared setting; not used by the declaration request. |

### Output and examples

The table/plain fields are `Identifier`, `ReadModel`, `Active`, `Rewindable`,
and `AutoMap`. Use plain output to enumerate identifiers before fetching one
declaration. JSON includes the full definitions and is much larger.

```bash
cratis chronicle projections list
cratis chronicle projections list -q
```

A successful empty list means no declarations were returned for that store.
Check the selected server/store and the application's registration before
concluding that a particular definition is absent. A connection or server error
means inspection did not complete. Replay does not register a missing definition.

## show

Shows one full declaration, including event mappings, child projections, and filters.

```bash
cratis chronicle projections show <IDENTIFIER>
```

### Arguments and options

| Argument or flag | Description |
| --- | --- |
| `IDENTIFIER` | Exact projection identifier returned by `list`; don't guess a class name. |
| `-e, --event-store <NAME>` | Event store to inspect. Resolves from the context, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Accepted shared setting; not used by the declaration request. |

### Output and examples

Replace `my-projection-id` with a returned identifier:

```bash
cratis chronicle projections show my-projection-id
cratis chronicle projections show my-projection-id -o json
```

The JSON wrapper contains `identifier`, `containerName`, and `declaration`.
`declaration` is a serialized definition **string**, not a nested object. An
unknown identifier returns a not-found error. Neither command registers a
projection or changes runtime state.

Inspect the declaration's target model and verify it against
[read-model definitions](read-models.md); the projection identifier is not
automatically a read-model identifier. Use [observers](observers.md) in the
selected namespace to inspect processing position and failures separately.
