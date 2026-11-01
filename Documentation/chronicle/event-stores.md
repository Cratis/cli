---
title: Event stores
description: List the event stores on a Chronicle server, the top-level containers for event sequences, namespaces and observers, with cratis chronicle event-stores.
---

An event store is the top-level container in Chronicle. It holds all event sequences, namespaces, observers, projections, and read models for a single logical application or domain boundary. A Chronicle server can host multiple event stores side by side, each fully isolated from the others.

## list

Lists all event stores registered on the connected Chronicle server.

```bash
cratis chronicle event-stores list
```

### Options

This command accepts only the global flags and the `--server` connection flag. It does not accept `-e` or `-n` because it operates above the event store level.

### Output

The default table output displays each event store name. Use `--output plain` for scripting:

```bash
cratis chronicle event-stores list --output plain
```

The plain format returns one event store name per line. A successful empty response means no stores were returned by that request; a connection, authentication, or server error means discovery did not complete. Neither result proves that an application's projection is missing.

### Examples

List all event stores in a table:

```bash
cratis chronicle event-stores list
```

List against a specific server:

```bash
cratis chronicle event-stores list --server chronicle://prod.example.com:35000
```

Choose one authorized store from the result, then discover its namespaces rather than automatically querying every store:

```bash
cratis chronicle namespaces list --server chronicle://prod.example.com:35000 -e myapp
```

Here `myapp` is an example—substitute the returned store name. Use the same explicit server for both requests, and select a returned namespace for subsequent [runtime inspection](observers.md). See the [namespaces reference](namespaces.md) for its output and [first inspection](../getting-started/index.mdx) for the complete workflow.
