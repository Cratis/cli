# Connection

This page documents how the `cratis` CLI connects to a Chronicle server: the connection string format and how the server address is resolved.

## Connection String Format

Chronicle connection strings use the `chronicle://` scheme:

```
chronicle://<host>:<port>/?<options>
```

**Examples:**

```
chronicle://localhost:35000
chronicle://prod.example.com:35000/
```

Chronicle serves gRPC and the OAuth/HTTP API over a single TLS port (default `35000`). TLS is mandatory on that port: in development, the server auto-generates a self-signed certificate for `localhost` and the CLI trusts it automatically — no certificate setup is required to connect to a local server.

### Options

| Option | Values | Description |
|---|---|---|
| `disableTls` | `true` / `false` | Disables TLS for the connection. Only needed when connecting through a plaintext-terminating proxy — the Chronicle server itself always serves TLS. |
| `certificatePath` | path | Path to a client certificate file, for pinning a specific expected server certificate. |
| `certificatePassword` | string | Password for the certificate file. |

## Resolution Order

When the CLI determines which server to connect to, it checks sources in this order and uses the first value it finds:

1. `--server` flag on the current command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active context in `~/.cratis/config.json`
4. Default: `chronicle://localhost:35000`

The `--server` flag always wins. The default applies only when none of the other sources provide a value.

## CHRONICLE_CONNECTION_STRING

Set this environment variable to provide a connection string without creating a named context. This is useful in CI pipelines, containers, and environments where managing a config file on disk is not practical:

```bash
export CHRONICLE_CONNECTION_STRING=chronicle://staging.example.com:35000/
cratis chronicle event-stores list
```

The variable is checked after `--server` but before the active context, so a context always overrides it when both are present unless `--server` is used explicitly.

## Per-Command Server Options

Every subcommand that communicates with Chronicle accepts this option directly, which takes precedence over context and environment variable values:

| Option | Description |
|---|---|
| `--server <CONNECTION_STRING>` | Chronicle server connection string |

**Example — connect to a specific server for a single command without changing the active context:**

```bash
cratis chronicle event-stores list --server chronicle://prod.example.com:35000/
```
