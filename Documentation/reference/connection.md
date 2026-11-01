---
title: Connection
description: "The cratis CLI connection reference: the chronicle:// connection string format and the order in which the CLI resolves which Chronicle server to use."
---

This page documents how the `cratis` CLI connects to a Chronicle server: the connection string format and how the server address is resolved.

## Connection String Format

Chronicle connection strings use the `chronicle://` scheme:

```text
chronicle://<host>:<port>/?<options>
```

**Examples:**

```text
chronicle://localhost:35000
chronicle://prod.example.com:35000/
```

Chronicle serves gRPC and the OAuth/HTTP API over a single TLS port (default `35000`). TLS is mandatory on that port: in development, the server auto-generates a self-signed certificate for `localhost` and the CLI trusts it automatically — no certificate setup is required to connect to a local server.

### Options

| Option | Values | Description |
|---|---|---|
| `skipTlsValidation` | `true` / `false` | Whether to skip validation of the server's TLS certificate. On by default, which is what lets the CLI trust a development server's self-signed certificate; set it to `false` to require a valid certificate. |
| `certificatePath` | path | Path to a PKCS#12 client certificate with its private key, used by login and all other Chronicle commands. With TLS validation enabled, a valid server certificate is accepted; a certificate with only chain errors is also accepted if its hash matches this certificate. Not a custom trust root. |
| `certificatePassword` | string | Password for the PKCS#12 client certificate, if it has one. |

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

The variable is checked after `--server` but before the active context, so the environment overrides the context when both are present. Given context A, environment B, and explicit flag C, the server is C. Remove the flag and it is B; remove the environment value too and it is A.

`context show` reports stored profile values, not the resolved server. Changing the active context does not remove an environment override. Avoid printing credential-bearing environment values while checking your target.

## Per-Command Server Options

Every subcommand that communicates with Chronicle accepts this option directly, which takes precedence over context and environment variable values:

| Option | Description |
|---|---|
| `--server <CONNECTION_STRING>` | Chronicle server connection string |

**Example — connect to a specific server for a single command without changing the active context:**

```bash
cratis chronicle event-stores list --server chronicle://prod.example.com:35000/
```

Use only an authorized target. The override does not edit the active profile. For commands scoped to an event store and namespace, also pass `-e <EVENT_STORE>` and `-n <NAMESPACE>` when you need a fully explicit target. Those values otherwise resolve from the context, then `default` and `Default`; changing only the server does not reset them.

Arc discovery uses a separate `--url` / `ARC_URL` resolution path. A Chronicle connection string is not an Arc application URL; see [Arc targeting](../arc/index.md).
