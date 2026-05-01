# Event Stores

An event store is the top-level container in Chronicle. It holds all event sequences, namespaces, observers, projections, and read models for a single logical application or domain boundary. A Chronicle server can host multiple event stores side by side, each fully isolated from the others.

## list

Lists all event stores registered on the connected Chronicle server.

```bash
cratis chronicle event-stores list
```

### Options

This command accepts only the global flags and the `--server` / `--management-port` connection flags. It does not accept `-e` or `-n` because it operates above the event store level.

### Output

The default table output displays each event store name. Use `--output plain` for scripting:

```bash
cratis chronicle event-stores list --output plain
```

The plain format returns one event store name per line, making it easy to pipe into other commands.

### Examples

List all event stores in a table:

```bash
cratis chronicle event-stores list
```

List against a specific server:

```bash
cratis chronicle event-stores list --server chronicle://prod.example.com:35000
```

Pipe the names into another command:

```bash
cratis chronicle event-stores list -q | xargs -I {} cratis chronicle namespaces list -e {}
```
