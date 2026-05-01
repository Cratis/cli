# Event Types

An event type is a registered schema that describes the shape of a domain event stored in Chronicle. Each event type has a name, a generation number, and a JSON Schema definition. Generations allow schemas to evolve over time while preserving older events.

## list

Lists all registered event types in the specified event store and namespace.

```bash
cratis chronicle event-types list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Output tip

Use `--output plain` when scanning for event type names. The JSON format includes the full JSON Schema definition for every event type, which can be up to 34 times larger than the plain output. Reserve JSON for when you need schema details.

```bash
cratis chronicle event-types list --output plain
```

### Examples

List all event types in the default event store:

```bash
cratis chronicle event-types list
```

List event types in a specific event store and namespace:

```bash
cratis chronicle event-types list -e myapp -n tenant-a
```

Get only the identifiers for scripting:

```bash
cratis chronicle event-types list -q
```

## show

Shows the full JSON Schema definition for a single event type.

```bash
cratis chronicle event-types show <EVENT_TYPE>
```

### Arguments

| Argument | Description |
|---|---|
| `EVENT_TYPE` | Event type identifier. Use the type name alone (e.g. `UserRegistered`) to get the latest generation, or append `+<generation>` (e.g. `UserRegistered+1`) to get a specific generation. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Examples

Show the latest schema for an event type:

```bash
cratis chronicle event-types show UserRegistered
```

Show a specific generation:

```bash
cratis chronicle event-types show UserRegistered+1
```

Get machine-readable JSON output:

```bash
cratis chronicle event-types show UserRegistered -o json
```
