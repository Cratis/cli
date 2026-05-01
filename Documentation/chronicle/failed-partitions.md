# Failed Partitions

A failed partition is an event source ID where an observer encountered an unhandled exception while processing an event. When a partition fails, Chronicle pauses that partition's processing and records the failure details — the failing sequence number, the exception message, and the number of attempts. All other partitions continue processing normally.

Fixing a failed partition typically involves correcting the application bug that caused the exception and then using `observers retry-partition` to resume processing.

## list

Lists all failed partitions across all observers, or for a specific observer.

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

Shows details about a specific failed partition, including the error message, the failing sequence number, and the attempt history.

```bash
cratis chronicle failed-partitions show <OBSERVER_ID> <PARTITION>
```

### Arguments

| Argument | Description |
|---|---|
| `OBSERVER_ID` | The observer that owns the failed partition. |
| `PARTITION` | The event source ID of the failed partition. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |
| `--detailed` | Show full stack traces and all recorded attempts. Defaults to `false`. |

By default, the command shows a truncated error message. Use `--detailed` to see the full stack trace and the history of all previous attempts. Use `--output json` to get the complete error information in a machine-readable form.

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

After reviewing a failed partition:

- Fix the application bug that caused the failure.
- Use `observers retry-partition` to resume the partition without replaying from scratch:

  ```bash
  cratis chronicle observers retry-partition my-observer-id user-42
  ```

- If the partition state is corrupt and must be rebuilt, use `observers replay-partition`:

  ```bash
  cratis chronicle observers replay-partition my-observer-id user-42
  ```
