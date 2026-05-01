# Namespaces

A namespace is an isolated tenant partition within an event store. All event sequences, observers, projections, and read models operate within a specific namespace. Separating tenants by namespace lets a single event store serve multiple customers or environments without sharing state.

The default namespace is `Default`. Most commands target `Default` unless you specify `-n`.

## list

Lists all namespaces within an event store.

```bash
cratis chronicle namespaces list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |

### Examples

List namespaces in the default event store:

```bash
cratis chronicle namespaces list
```

List namespaces in a named event store:

```bash
cratis chronicle namespaces list -e myapp
```

List in plain format for scripting:

```bash
cratis chronicle namespaces list -e myapp --output plain
```

List namespaces across all event stores:

```bash
cratis chronicle event-stores list -q | xargs -I {} cratis chronicle namespaces list -e {}
```
