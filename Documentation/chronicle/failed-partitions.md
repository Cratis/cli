---
description: List and inspect the failed partitions where a Chronicle observer hit an unhandled exception, with cratis chronicle failed-partitions.
---

# Failed Partitions

When an observer cannot process an event, its failed-partition record tells you where it stopped and why: the partition key, failing sequence number, exception, and attempts. Chronicle pauses processing for that partition; other partitions continue normally. The key is often an event source ID, but it is not always one. Use the returned key with the observer ID to inspect the failure rather than guessing from a model's name or key.

Start with `failed-partitions list`, then use `show` for the affected partition. Fix the cause before choosing retry, quarantine clearing, or replay; these are different actions, not a sequence to run through. The [stuck-observer guide](../scenarios/fix-a-stuck-observer.md) explains how to inspect one target and choose recovery with its owner.

## list

Lists failed partitions in the selected event store and namespace, across all observers or filtered by `--observer`. Confirm the authorized server and context first; the examples below otherwise use the CLI's resolved target.

```bash
cratis chronicle failed-partitions list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |
| `--observer <OBSERVER_ID>` | Filter to a specific observer. |

### Examples

List all failed partitions:

```bash
cratis chronicle failed-partitions list
```

List failed partitions for a specific observer:

```bash
cratis chronicle failed-partitions list --observer my-observer-id
```

Plain output for scripting:

```bash
cratis chronicle failed-partitions list --output plain
```

## show

Use the observer ID and partition key returned by `list` to read the error, failing sequence number, and attempt history. Those details help the application owner locate the input or handler that failed. If `show` returns not-found, recheck the IDs and current failure list; it does not prove that the expected view is correct. A failed query means the inspection could not run.

```bash
cratis chronicle failed-partitions show <OBSERVER_ID> <PARTITION>
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer that owns the failed partition. |
| `PARTITION` | The returned partition key, typically an event source ID. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |
| `--detailed` | Show full stack traces and all recorded attempts. Defaults to `false`. |

By default, the command truncates error details. Use `--detailed` for the full stack trace and all recorded attempts, or `--output json` for complete machine-readable error information. Keep credentials, sensitive payloads, and stack traces out of shared logs.

### Examples

Show the failed partition summary:

```bash
cratis chronicle failed-partitions show my-observer-id user-42
```

Show full stack traces and all attempt history:

```bash
cratis chronicle failed-partitions show my-observer-id user-42 --detailed
```

Get the full error as JSON:

```bash
cratis chronicle failed-partitions show my-observer-id user-42 -o json
```

### Next Steps

Have the application owner fix the cause, whether it is a code bug, bad input, or an unavailable dependency. Then agree on one action for the confirmed server, store, namespace, observer, and partition, including any repeated handler effects. Choose according to the partition's state:

- If accumulated state is still valid and the partition is failed but not quarantined, an approved `observers retry-partition` continues without rebuilding it:

  ```bash
  cratis chronicle observers retry-partition my-observer-id user-42
  ```

- If the partition is quarantined after exhausting its retries, `retry-partition` refuses it. The `clear-partition-quarantine` command below resets the retry budget **and starts a retry**, so approval must cover both clearing and rerunning the handler, including repeated external effects. Add `--no-retry` when only clearing is approved. Requires Chronicle 19.29.0 or later. `observers clear-quarantine` clears only the observer; individually quarantined partitions remain quarantined:

  ```bash
  cratis chronicle failed-partitions list --observer my-observer-id
  cratis chronicle observers clear-partition-quarantine my-observer-id user-42
  ```

- If the accumulated state itself is invalid and needs rebuilding, agree on `observers replay-partition`. It prompts before deleting and rebuilding that partition's state; it is not a workaround for a refused retry:

  ```bash
  cratis chronicle observers replay-partition my-observer-id user-42
  ```

After recovery starts, inspect the same observer and failure list once more, then check the relevant model/key. For a clear-only action, inspect the remaining state before agreeing on a retry. A successful request alone cannot establish that processing finished or the expected view is now correct.
