---
title: Closed streams
description: List closed stream scopes and repair an exact mistaken manual closure with an audited reason.
---

Requires Chronicle 19.39.0 or later. Both commands operate on the event sequence selected by `--sequence` (default: `event-log`), within the active event store and namespace. Override those with `--event-store` and `--namespace`.

## closed list

Lists closed scopes and their closure metadata. Filters select closures within the supplied dimensions; they do not test whether a broader closure covers a particular stream. The query is not paged and returns every match.

```bash
cratis chronicle streams closed list
cratis chronicle streams closed list --event-source-id order-42 -o json
```

| Option | Description | Default |
|---|---|---|
| `--sequence <ID>` | Event sequence name | `event-log` |
| `--event-source-id <ID>` | Filter by event source identifier | Unset |
| `--event-source-type <TYPE>` | Filter by event source type | Unset |
| `--event-stream-type <TYPE>` | Filter by event stream type | Unset |
| `--event-stream-id <ID>` | Filter by event stream identifier | Unset |

Table and plain output show the four scope dimensions, `Origin`, `ClosedBy`, `SequenceNumber` and `ClosedAt`. JSON uses the corresponding camelCase names. `origin` is `CompleteStream` for a manual closure or `ClosingEvent` for an event-driven closure. `closedBy` names the owning constraint and is absent for manual closures. Older closure records may lack a closing time and use the unavailable sequence number (`18446744073709551615`). Absent values are empty cells in table/plain output and omitted in JSON.

## closed reopen

Repairs only an **exact manual closure**. Supply all and only the dimensions shown on that closure; the command never widens, narrows or splits it. At least one dimension and a non-empty reason are required.

```bash
cratis chronicle streams closed reopen --event-source-id order-42 --reason "Closed the wrong order"
```

Accepts the same sequence and dimension options as `closed list`, but the dimensions identify the exact closure rather than filter results.

| Additional option | Description |
|---|---|
| `--reason <TEXT>` | Required repair reason recorded in the system event sequence |
| `-y, --yes` | Skip the confirmation prompt after checking the exact target; required in non-interactive environments |

The server requires an authenticated principal with a subject. It records the reason and authenticated actor in a `StreamScopeReopened` system event **before** removing the manual closure. The audit contains the event source id; treat it as personal data if that id identifies a person.

Success confirms the exact repair, not that appends are now allowed: other covering closures may remain. List closures again to inspect them.

| Failure | Next step |
|---|---|
| No exact closure (`NotCompleted`) | List closures and check every dimension, including those left unset. Nothing was reopened. |
| Event-driven closure (`ClosedByEvent`) | Append one of the owning closing constraint's `ReopenedBy` events instead. The repair command cannot remove it. |
| Missing reason or scope dimensions | Supply a non-empty `--reason` and at least one dimension. |
| Authorization, validation or server failure | Check credentials, the scope and the reported server message. No success is claimed. |
| Unsupported server RPC | Upgrade Chronicle to 19.39.0 or later. |

All failures return a non-zero exit code. For the closure rules, see [Inspecting and repairing closed streams](/chronicle/closing-streams/inspecting-and-repairing/).
