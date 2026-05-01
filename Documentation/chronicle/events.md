# Events

The `events` commands let you query events from an event sequence and retrieve the highest used sequence number. Use these commands to verify that events were appended, inspect their content, and understand the current state of an event log.

## get

Retrieves events from an event sequence, with optional filtering by range, event source, and event type.

```bash
cratis chronicle events get
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to query. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to query. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence to query. Defaults to the event log. |
| `--from <SEQUENCE_NUMBER>` | Start of the sequence range (inclusive, 0-based). Defaults to `0`. |
| `--to <SEQUENCE_NUMBER>` | End of the sequence range (inclusive). Omit to read to the current tail. |
| `--event-source-id <ID>` | Filter events to a single event source. |
| `--event-type <TYPES>` | Comma-separated list of event type names or `name+generation` pairs to filter by. |

### Output tip

Use `--output plain` when processing events in scripts. The plain format is approximately 25 times smaller than JSON because it omits embedded metadata, context headers, and schema details. Use `--output json` when you need the full event document including metadata.

### Examples

Get all events from the event log:

```bash
cratis chronicle events get
```

Get events from sequence number 100 to 200:

```bash
cratis chronicle events get --from 100 --to 200
```

Get all events for a specific event source:

```bash
cratis chronicle events get --event-source-id "user-42"
```

Filter to a single event type:

```bash
cratis chronicle events get --event-type UserRegistered
```

Filter to a specific generation of an event type:

```bash
cratis chronicle events get --event-type UserRegistered+2
```

Filter to multiple event types:

```bash
cratis chronicle events get --event-type UserRegistered,UserEmailChanged
```

Combine filters for targeted inspection:

```bash
cratis chronicle events get --event-source-id "user-42" --event-type UserRegistered --output json
```

## tail

Gets the highest used sequence number in an event sequence. This is the tail of the log — the sequence number of the last appended event. It is not a count of events: gaps may exist in the sequence if events were redacted or sequences were not used contiguously.

```bash
cratis chronicle events tail
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to query. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to query. Defaults to `Default`. |
| `--sequence <NAME>` | Event sequence to query. Defaults to the event log. |
| `--event-type <TYPE>` | Return the tail sequence number for a specific event type. |
| `--event-source-id <ID>` | Return the tail sequence number for a specific event source. |

### Output

With `--output plain`, the command returns a single number on one line, making it easy to capture in a script:

```bash
TAIL=$(cratis chronicle events tail --output plain)
```

### Examples

Get the tail sequence number of the event log:

```bash
cratis chronicle events tail
```

Get the tail for a specific event source:

```bash
cratis chronicle events tail --event-source-id "user-42"
```

Get the tail for a specific event type:

```bash
cratis chronicle events tail --event-type UserRegistered
```

Capture the tail number in a script:

```bash
TAIL=$(cratis chronicle events tail -e myapp --output plain)
echo "Current tail: $TAIL"
```
