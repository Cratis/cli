---
title: Context
description: Create, switch and manage cratis contexts, named connection profiles stored on disk, to move between Chronicle servers without retyping connection strings.
---

A context is a named connection profile stored locally on disk. Contexts let you switch between multiple Chronicle servers — development, staging, production — without retyping connection strings on every command.

## Configuration File

Saved contexts are stored in `~/.cratis/config.json`. On a fresh install, running bare `cratis` in an interactive terminal creates and saves a built-in `default` context. Other configuration reads can use an in-memory default without saving it. You can inspect the file location with `cratis context path`.

Creating, selecting, renaming, deleting, or editing a context changes local configuration, not Chronicle state. Connection strings and saved credentials are sensitive. Do not commit the file or paste it into a log, issue, or assistant prompt.

## Connection Resolution Order

When the CLI resolves which server to connect to, it checks in this order:

1. `--server` flag on the current command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active context in `~/.cratis/config.json`
4. Default: `chronicle://localhost:35000`

For example, context server A plus environment server B selects B; adding explicit `--server C` selects C. `context show` displays stored profile values, not an effective remote target or a connection test. See the [connection reference](../reference/connection.md) for one-command selection.

Event store and namespace resolve independently: explicit `-e` / `-n`, then the active context's values, then `default` / `Default`. Definition commands such as projection/read-model listing operate at event-store scope even though they accept the shared namespace option.

## Commands

### list

Lists all configured contexts. The active context is marked with an asterisk.

```bash
cratis context list
```

Use `--output plain` for scripting — it emits tab-separated rows without decorations:

```bash
cratis context list -o plain
```

Example output:

```text
* dev    chronicle://localhost:35000
  prod   chronicle://prod.example.com:35000/
```

---

### create

Creates a named context. If it is the only saved context or no active context is set, creation selects it automatically. If interactive first-run setup already created `default`, creating another profile does not switch away from it; use `context set <NAME>` explicitly.

```bash
cratis context create <NAME> --server <CONNECTION_STRING>
```

**Options:**

| Option | Short | Description |
|---|---|---|
| `--server` | | Chronicle server connection string |
| `--event-store` | `-e` | Default event store for this context (default: `default`) |
| `--namespace` | `-n` | Default namespace for this context (default: `Default`) |

**Example — local development server:**

```bash
cratis context create dev --server chronicle://localhost:35000
```

**Example — staging server with a specific event store and namespace:**

```bash
cratis context create staging \
  --server chronicle://staging.example.com:35000/ \
  --event-store orders \
  --namespace Production
```

---

### set

Switches the locally stored active context. Subsequent Chronicle commands use it where explicit options and the server environment variable do not override it. Arc uses its separate application URL, not this context.

```bash
cratis context set <NAME>
```

The command validates that the named context exists before activating it. If the name does not match any stored context, it reports an error.

**Example:**

```bash
cratis context set prod
```

Before switching, note the active name from `context list`. To return, run `context set <PREVIOUS_CONTEXT>` with that name. A one-off `--server` override avoids switching, but does not replace the context's event-store or namespace defaults—pass `-e` and `-n` too when needed.

---

### show

Displays the details of the currently active context: server address, event store, namespace, and credentials status.

```bash
cratis context show
```

Use `--output json` for machine-readable key-value output:

```bash
cratis context show -o json
```

Example output (plain):

```text
Server:       chronicle://localhost:35000
Event store:  default
Namespace:    Default
Client ID:    (not set)
```

---

### delete

Removes a named context from the configuration file.

```bash
cratis context delete <NAME>
```

You cannot delete the active context or the built-in default context. Switch to a different context first, then delete the one you no longer need:

```bash
cratis context set dev
cratis context delete staging
```

The command asks for confirmation and defaults to **No**. In automation or another non-interactive environment, pass `--yes` explicitly or the command returns a nonzero validation exit code without deleting the context.

---

### rename

Renames an existing context. If you rename the active context, the CLI updates the active context reference automatically so you remain connected.

```bash
cratis context rename <OLD_NAME> <NEW_NAME>
```

**Example:**

```bash
cratis context rename dev local
```

---

### path

Prints the absolute path to the configuration file. Useful for inspecting, backing up, or diffing the config:

```bash
cratis context path
```

Example output:

```text
/Users/alice/.cratis/config.json
```

---

### set-value

Sets an individual value on the current context without replacing the entire context.

```bash
cratis context set-value <KEY> <VALUE>
```

**Valid keys:**

| Key | Description |
|---|---|
| `server` | Chronicle server connection string |
| `event-store` | Default event store name |
| `namespace` | Default namespace name |
| `client-id` | OAuth client ID for authenticated servers |
| `client-secret` | OAuth client secret (masked in output) |

**Example — update the server URL:**

```bash
cratis context set-value server chronicle://localhost:35000
```

**Example — set credentials for an authenticated server:**

```bash
cratis context set-value client-id my-app
cratis context set-value client-secret s3cr3t
```

The client secret is stored in the config file and masked in context detail output. Masking a field is not a guarantee that an entire connection string or debug log is safe to share. Protect file access and avoid putting real secrets in shell history; use your environment's approved credential procedure.
