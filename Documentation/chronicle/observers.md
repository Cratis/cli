---
title: Inspect and recover Cratis Chronicle observers
description: Inspect Chronicle observers, the reactors, reducers and projections that consume events, and replay or recover them with cratis chronicle observers.
---

<a id="observers"></a>

Observers are the processing units that consume events from an event sequence and produce side effects or projections. Chronicle supports three kinds of observer: reactors (side effects), reducers (compute read model state), and projections (declarative read model builders). Each observer tracks its position in the event sequence and pauses a partition when it encounters a failure.

## list

Lists all observers registered in the specified event store and namespace, including their current state, quarantine status, and sequence position.

```bash
cratis chronicle observers list
```

The last column, `StateChangedAt`, shows when the observer's running state last changed to its current value, as an ISO 8601 UTC time. It is not a heartbeat: an observer that stays in the same state keeps its original time. When the server does not report a time, the table shows `-` and the JSON output omits the `runningStateChangedAt` property.

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to the active context's namespace, then `Default`. |
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

## show

Shows detailed information about a single observer, including its event type subscriptions, current state, when that state last changed (`StateChangedAt`, or `runningStateChangedAt` in JSON; omitted from JSON and shown as `-` when the server does not report it), and sequence position.

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
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to the active context's namespace, then `Default`. |
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

The command prompts for confirmation before proceeding. Pass `--yes` to skip the prompt in automated workflows.

### Replaying a subscription observer

An event store subscription forwards events from the producer's outbox to a consumer's Inbox through a forwarding observer in the producer event store. Its identity is `<actual-subscription-id>-><consumer-store>`, where the first part is the subscription's own identifier and is not necessarily the producer event store name.

The CLI treats a replay as a subscription replay when the observer identifier contains `->` or the observer reads from the `outbox` event sequence. For such a replay:

- You must pass the producer event store and namespace explicitly with `-e/--event-store` and `-n/--namespace`; the active context is not used. Without them the command exits with a validation error and starts nothing.
- The confirmation prompt warns that the replay may append duplicate events to the consumer Inbox. Declining the prompt starts no replay.
- When the CLI only learns from the server that the observer reads from `outbox`, it exits with a validation error and starts nothing unless you pass `--yes`. Re-run with `--sequence outbox` to see the warning prompt instead.

Replay is not a deduplicated repair. The subscription behaviour it relies on is:

- A new forwarding observer starts at sequence number 0; a reused one keeps its persisted position.
- A replay starts asynchronous forwarding from sequence number 0, and the command returns before it completes.
- Only source events still retained in the producer's outbox and matching the subscription's filters can be forwarded.
- Events the consumer Inbox already holds may be appended again.

```bash
cratis chronicle observers replay 'orders-subscription->consumer-store' -e producer-store -n Default
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer to replay. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
| `--sequence <ID>` | Event sequence the observer reads from. Defaults to the observer's own sequence. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Replay an observer interactively:

```bash
cratis chronicle observers replay my-observer-id
```

Replay without prompting (automation):

```bash
cratis chronicle observers replay my-observer-id --yes
```

Replay all observers (use with caution):

```bash
cratis chronicle observers list -q | xargs -I {} cratis chronicle observers replay {} --yes
```

## replay-partition

Replays a single partition of an observer from sequence number zero for that partition only. This is less disruptive than a full replay because it leaves all other partitions intact.

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
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Replay a specific partition:

```bash
cratis chronicle observers replay-partition my-observer-id user-42
```

## retry-partition

Retries a failed partition without replaying it from the beginning. The observer attempts to process the event that last caused the failure. Use this after fixing the underlying bug — it is the preferred recovery path for transient errors and corrected application bugs.

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
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
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
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
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
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
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
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
| `--sequence <NAME>` | Event sequence. Defaults to `event-log`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Clear quarantine for an observer:

```bash
cratis chronicle observers clear-quarantine my-observer-id
```

## Troubleshooting Workflow

Use this sequence when an observer appears stuck, behind, or failing:

1. Check the observer list for state and sequence numbers:

   ```bash
   cratis chronicle observers list --output plain
   ```

2. Inspect the specific observer for detailed state:

   ```bash
   cratis chronicle observers show my-observer-id -o json
   ```

3. Check for failed partitions:

   ```bash
   cratis chronicle failed-partitions list --output plain
   ```

4. Inspect the specific failed partition and its error:

   ```bash
   cratis chronicle failed-partitions show my-observer-id user-42
   ```

5. Fix the underlying application bug, then retry the partition:

   ```bash
   cratis chronicle observers retry-partition my-observer-id user-42
   ```

6. If the read model state is known to be corrupt and cannot be recovered by retry, replay the partition:

   ```bash
   cratis chronicle observers replay-partition my-observer-id user-42
   ```

7. Only as a final resort — if all partitions are corrupt or the observer schema changed incompatibly — replay the entire observer:

   ```bash
   cratis chronicle observers replay my-observer-id
   ```
