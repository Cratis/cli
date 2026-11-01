---
title: Observers
description: Inspect Chronicle observer state and positions, then distinguish targeted recovery from destructive replay.
---

Observers are the processing units that consume events from an event sequence and produce side effects or projections. Chronicle supports three kinds of observer: reactors (side effects), reducers (compute read model state), and projections (declarative read model builders). Each observer tracks its position in the event sequence and pauses a partition when it encounters a failure.

Use [Find and fix a stuck observer](../scenarios/fix-a-stuck-observer.md) for the investigation order. Confirm the authorized server, event store, namespace, observer ID, and partition before changing anything. The commands below are reference entries, not a recovery script to run in order.

A diagnostic finding is not permission to recover. Retries and replays re-run handlers and can repeat external side effects. Obtain approval for the exact target and action, and fix the underlying cause first. An assistant must not supply `--yes` merely to overcome a prompt or non-interactive failure.

## list

Lists all observers registered in the specified event store and namespace, including their current state, quarantine status, and sequence position.

```bash
cratis chronicle observers list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |
| `-t, --type <TYPE>` | Filter by observer type: `all`, `reactor`, `reducer`, `projection`. Defaults to `all`. |

### Examples

List all observers:

```bash
cratis chronicle observers list
```

List only projection observers:

```bash
cratis chronicle observers list --type projection
```

List observers in plain format for scripting:

```bash
cratis chronicle observers list --output plain
```

Get observer IDs for piping:

```bash
cratis chronicle observers list -q
```

A successful empty list means no matching observers were returned for that namespace/type. A failed request means state is unknown, not empty. Positions describe this observer's subscriptions; equality with the global event-log tail is not a general completion test. Corroborate the relevant event and resulting model state.

## show

Shows detailed information about a single observer, including its event type subscriptions, current state, and sequence position.

```bash
cratis chronicle observers show <OBSERVER_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer identifier. Use `observers list -q` to retrieve identifiers. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence the observer reads from. Defaults to the event log. |

### Examples

Show observer details in a table:

```bash
cratis chronicle observers show my-observer-id
```

Get full observer detail as JSON:

```bash
cratis chronicle observers show my-observer-id -o json
```

## replay

Replays an observer from sequence number zero, re-processing every event it subscribes to. This is a destructive operation: the observer discards all accumulated state and rebuilds it from scratch.

Use replay only as a last resort — for example, when an observer's read model is known to be corrupt and cannot be recovered by a partition retry.

```bash
cratis chronicle observers replay <OBSERVER_ID>
```

**Whole-observer replay starts without an interactive confirmation prompt.** Confirm the exact target and obtain approval before executing it; a prompt will not stop an accidental invocation. The inherited `--yes` option has no confirmation to skip here. Partition replay, by contrast, prompts first.

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The confirmed observer identifier to replay. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence the observer reads from. Defaults to `event-log`. |

### Examples

Start the explicitly approved replay:

```bash
cratis chronicle observers replay my-observer-id
```

Do not pipe a discovered observer list into replay as a default investigation step. Approve each intended target and impact; widening a single-observer recovery to the whole namespace requires a separate decision.

## replay-partition

Replays a single partition of an observer from sequence number zero for that partition only. This limits the rebuild to that partition and leaves other partitions intact, but can still re-run handlers and their external effects. Prefer retry when existing state is valid.

```bash
cratis chronicle observers replay-partition <OBSERVER_ID> <PARTITION>
```

The command prompts for confirmation before proceeding.

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer whose partition to replay. |
| `PARTITION` | The event source ID identifying the partition. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Replay a specific partition:

```bash
cratis chronicle observers replay-partition my-observer-id user-42
```

## retry-partition

Retries a failed partition without replaying it from the beginning. The observer attempts to process the event that last caused the failure. Use this after fixing the underlying bug — it is the preferred recovery path for transient errors and corrected application bugs when accumulated state remains valid. It prompts for confirmation. A successful response means recovery started, not that it finished; re-check the target's failures and relevant state once. A refused retry exits non-zero and explains whether the observer/partition is quarantined or the partition is not failed.

```bash
cratis chronicle observers retry-partition <OBSERVER_ID> <PARTITION>
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer whose partition to retry. |
| `PARTITION` | The event source ID identifying the failed partition. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Retry a failed partition:

```bash
cratis chronicle observers retry-partition my-observer-id user-42
```

## retry-failed-partitions

Retries every failed partition of an observer in one operation, instead of retrying them one at a time with `retry-partition`. The kernel starts recovery a bounded batch of partitions at a time, so retrying many of them never overwhelms the observer or the event store. The command returns as soon as recovery is started and reports how many partitions were failed at that moment; it does not wait for recovery to finish. Check progress with `failed-partitions list`.

```bash
cratis chronicle observers retry-failed-partitions <OBSERVER_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer whose failed partitions to retry. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `--sequence <ID>` | Event sequence name. Defaults to `event-log`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Retry all failed partitions of an observer:

```bash
cratis chronicle observers retry-failed-partitions my-observer-id
```

An observer with no failed partitions is reported as such and exits zero, because there was nothing to do. Requires Chronicle 19.80.0 or later.

## clear-partition-quarantine

Clears the quarantine of one failed partition. A partition is quarantined after it exhausts its retry attempts; `retry-partition` refuses it until the quarantine is cleared. The command resets the partition's retry budget, keeps its attempt history, and by default starts a retry from the failed sequence number.

```bash
cratis chronicle observers clear-partition-quarantine <OBSERVER_ID> <PARTITION>
```

Requires Chronicle 19.29.0 or later. The command prompts for confirmation before proceeding.

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer whose partition to release. |
| `PARTITION` | The event source ID identifying the quarantined partition. |

### Options

| Flag | Description |
|---|---|
| `--no-retry` | Only clear the quarantine; do not start a retry. |
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence. Defaults to `event-log`. |
| `-y, --yes` | Skip confirmation prompt. |

The command exits non-zero when nothing was cleared: the partition is not found, it is not quarantined (use `retry-partition`), or the observer itself is quarantined so the retry did not start (run `clear-quarantine` on the observer, then `retry-partition`).

> [!WARNING]
> A retry re-runs the handler, so a side effect that already happened (an email sent, a call made) will happen again. See the [Chronicle reactor delivery identity documentation](https://github.com/Cratis/Chronicle/tree/main/Documentation) for making handlers safe to re-run.

### Examples

```bash
cratis chronicle observers clear-partition-quarantine my-observer-id user-42
cratis chronicle observers clear-partition-quarantine my-observer-id user-42 --no-retry
```

## clear-quarantine

Clears quarantine for a quarantined observer so it can resume processing. This is observer-level only: partitions that are individually quarantined stay quarantined. Use [`clear-partition-quarantine`](#clear-partition-quarantine) for those.

```bash
cratis chronicle observers clear-quarantine <OBSERVER_ID>
```

The command prompts for confirmation before proceeding.

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer whose quarantine should be cleared. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence. Defaults to `event-log`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Clear quarantine for an observer:

```bash
cratis chronicle observers clear-quarantine my-observer-id
```

## Troubleshooting workflow

Follow [Find and fix a stuck observer](../scenarios/fix-a-stuck-observer.md) for a scoped, finite investigation:

1. Run one selected-scope diagnostic sweep; distinguish findings from unavailable checks.
2. Inspect the exact observer and failed partition. Confirm the cause with the application owner.
3. Fix that cause before considering recovery. If accumulated state remains valid, prefer a targeted retry; if not, discuss a partition rebuild. Whole-observer replay has a larger blast radius.
4. Obtain approval for the exact action and scope; these alternatives are not a sequence to run automatically.
5. Make one fresh inspection of the target and relevant model state. If it is still running or failing, agree on a bounded later check or stop and report—don't widen or repeat recovery indefinitely.
