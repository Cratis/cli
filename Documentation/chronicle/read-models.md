---
title: Read models
description: Identify a read model and its owner, then inspect a bounded page or exact key in a Chronicle namespace.
---

A read model is state built from events by an observer. Server-owned models store
instances in Chronicle; client-owned models keep state in the owning application.
An instance key is typically an event source ID, but depends on the mapping.

Use [Find a missing projection](../scenarios/find-a-missing-projection.md) to connect
a model definition to its observer and namespace. The CLI cannot retrieve
client-owned state through Chronicle: stop server-side instance retrieval for
those models and inspect the owning application instead.

## list

Lists definitions registered in the specified **event store**, not a namespace.
The request uses the event store only. The other commands on this page query
runtime data in the selected event store and namespace.

```bash
cratis chronicle read-models list --output plain
```

### Options

| Flag | Description |
| --- | --- |
| `-e, --event-store <NAME>` | Event store. Context value, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Accepted shared setting; not used by this definition request. |

### Output

The table columns are `Identifier`, `Container`, `DisplayName`, `ObserverType`,
`Owner`, `Queryable`, and `Source`. `Queryable: No` means state is client-owned
and unavailable through Chronicle's instance explorer.

JSON is a definition DTO array with `identifier`, `generation`, `containerName`,
`displayName`, `observerType`, `observerIdentifier`, `owner`, `queryable`, and
`source`. It is not full raw type metadata. `observerIdentifier` connects the
definition to its runtime observer.

Use **`identifier`** for subsequent lookups. A display name, container name,
class name, or projection identifier is not interchangeable with it. Plain output
is useful for discovery; JSON supplies the additional ownership/link fields.

```bash
cratis chronicle read-models list -o json
cratis chronicle read-models list -q
```

A successful empty list means no definitions were returned for the selected
store. Check registration and targeting rather than replaying. A connection or
server error means inspection did not complete.

## instances

Lists stored instances of a queryable model, with pagination. Start with one
small page rather than enumerating all pages.

```bash
cratis chronicle read-models instances <READ_MODEL> --page 0 --page-size 5
```

### Arguments and options

| Argument or flag | Description |
| --- | --- |
| `READ_MODEL` | Exact read-model identifier from `list`. |
| `-e, --event-store <NAME>` | Event store. Context value, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Namespace. Context value, then `Default`, when omitted. |
| `--page <NUMBER>` | Zero-based page index. Defaults to `0`. |
| `--page-size <COUNT>` | Instances per page. Defaults to `20`. |

JSON wraps `totalCount`, `page`, `pageSize`, and `instances`. A successful empty
page differs from a failed query or client-owned data being unavailable. Check
page, namespace, model identifier, and ownership before interpreting absence.

### Examples

Here and below, `UserProfile` must be the actual identifier returned by `list`:

```bash
cratis chronicle read-models instances UserProfile --page 0 --page-size 5 -o json
cratis chronicle read-models instances UserProfile --page 1 --page-size 50
```

The second request is optional: make it only when the larger page is needed and
within your inspection budget. Payloads can contain sensitive application data.

## get

Gets one server-queryable instance by its exact key.

```bash
cratis chronicle read-models get <READ_MODEL> <KEY>
```

### Arguments and options

| Argument or flag | Description |
| --- | --- |
| `READ_MODEL` | Exact read-model identifier from `list`. |
| `KEY` | Instance key from the model mapping, typically an event source ID. |
| `-e, --event-store <NAME>` | Event store. Context value, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Namespace. Context value, then `Default`, when omitted. |
| `--sequence <NAME>` | Event sequence associated with the model. Defaults to `event-log`. |

### Examples

```bash
cratis chronicle read-models get UserProfile user-42
cratis chronicle read-models get UserProfile user-42 -o json
```

## occurrences

Gets the occurrence count for a read-model type and generation in the selected
namespace. This is not an all-generations instance count.

```bash
cratis chronicle read-models occurrences <READ_MODEL_TYPE>
```

### Arguments and options

| Argument or flag | Description |
| --- | --- |
| `READ_MODEL_TYPE` | Read-model type identifier from `list`. |
| `-e, --event-store <NAME>` | Event store. Context value, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Namespace. Context value, then `Default`, when omitted. |
| `--generation <NUMBER>` | Generation to count. Defaults to `1`. |

### Examples

```bash
cratis chronicle read-models occurrences UserProfile
cratis chronicle read-models occurrences UserProfile --generation 2
```

## snapshots

Retrieves snapshots for one exact model/key pair, including state and the
sequence position captured. This command has no pagination option; choose the
key deliberately and keep payloads out of shared logs or assistant prompts.

```bash
cratis chronicle read-models snapshots <READ_MODEL> <KEY>
```

### Arguments and options

| Argument or flag | Description |
| --- | --- |
| `READ_MODEL` | Exact read-model identifier from `list`. |
| `KEY` | Instance key, typically an event source ID. |
| `-e, --event-store <NAME>` | Event store. Context value, then `default`, when omitted. |
| `-n, --namespace <NAME>` | Namespace. Context value, then `Default`, when omitted. |
| `--sequence <NAME>` | Event sequence associated with the model. Defaults to `event-log`. |

### Examples

```bash
cratis chronicle read-models snapshots UserProfile user-42
cratis chronicle read-models snapshots UserProfile user-42 -o json
```

A stored instance is evidence of model state, not proof that every relevant event
was handled. For stale state, inspect the linked observer and its failures using
[the recovery investigation](../scenarios/fix-a-stuck-observer.md) before proposing
any retry or replay.
