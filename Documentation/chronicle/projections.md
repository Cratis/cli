# Projections

A projection is a declarative read-model builder driven by events. It defines how event data maps onto a read model: which fields to set, how to handle collections, when to create or delete instances, and which event types to subscribe to. Chronicle evaluates projections as events arrive and keeps the resulting read models up to date.

## list

Lists all projection definitions registered in the specified event store and namespace.

```bash
cratis chronicle projections list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Output tip

Use `--output plain` when you need to enumerate projections by name. The JSON format includes the full projection definition blob for each entry, which is significantly larger than the plain output. Use JSON only when you need the complete definition.

```bash
cratis chronicle projections list --output plain
```

### Examples

List all projections:

```bash
cratis chronicle projections list
```

List in a specific event store:

```bash
cratis chronicle projections list -e myapp
```

Get projection identifiers for scripting:

```bash
cratis chronicle projections list -q
```

## show

Shows the full projection declaration for a single projection, including all event mappings, child projections, and filters.

```bash
cratis chronicle projections show <IDENTIFIER>
```

### Arguments

| Argument | Description |
|---|---|
| `IDENTIFIER` | The projection identifier. Use `projections list -q` to retrieve identifiers. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Examples

Show a projection definition:

```bash
cratis chronicle projections show my-projection-id
```

Get the full definition as JSON:

```bash
cratis chronicle projections show my-projection-id -o json
```
