# Connection

This page documents how the `cratis` CLI connects to a Chronicle server: the connection string format, how the server address is resolved, and the management port used for HTTP operations.

## Connection String Format

Chronicle connection strings use the `chronicle://` scheme:

```
chronicle://<host>:<port>/?<options>
```

**Examples:**

```
chronicle://localhost:35000/?disableTls=true
chronicle://prod.example.com:35000/
```

### Options

| Option | Values | Description |
|---|---|---|
| `disableTls` | `true` / `false` | Disables TLS for the gRPC connection. Required for local servers that do not have a certificate configured. |

## Resolution Order

When the CLI determines which server to connect to, it checks sources in this order and uses the first value it finds:

1. `--server` flag on the current command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active context in `~/.cratis/config.json`
4. Default: `chronicle://localhost:35000/?disableTls=true`

The `--server` flag always wins. The default applies only when none of the other sources provide a value.

## CHRONICLE_CONNECTION_STRING

Set this environment variable to provide a connection string without creating a named context. This is useful in CI pipelines, containers, and environments where managing a config file on disk is not practical:

```bash
export CHRONICLE_CONNECTION_STRING=chronicle://staging.example.com:35000/
cratis event-stores list
```

The variable is checked after `--server` but before the active context, so a context always overrides it when both are present unless `--server` is used explicitly.

## Management Port

Some Chronicle operations — including authentication token exchange and certain HTTP management APIs — use a separate HTTP port rather than the gRPC port in the connection string.

The default management port is `8080`.

Override it with the `--management-port` flag, which is available on all `cratis` subcommands that communicate with the server:

```bash
cratis auth status --management-port 9090
```

You can also persist the management port in a context so you do not need to specify it on every command:

```bash
cratis context set-value management-port 9090
```

## Per-Command Server Options

Every subcommand that communicates with Chronicle accepts these options directly, which take precedence over context and environment variable values:

| Option | Description |
|---|---|
| `--server <CONNECTION_STRING>` | Chronicle server connection string |
| `--management-port <PORT>` | HTTP management port (default: `8080`) |

**Example — connect to a specific server for a single command without changing the active context:**

```bash
cratis event-stores list --server chronicle://prod.example.com:35000/
```
