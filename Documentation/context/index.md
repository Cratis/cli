# Context

A context is a named connection profile stored locally on disk. Contexts let you switch between multiple Chronicle servers — development, staging, production — without retyping connection strings on every command.

## Configuration File

All contexts are stored in `~/.cratis/config.json`. The CLI creates this file automatically when you create your first context. You can inspect its location with `cratis context path`.

## Connection Resolution Order

When the CLI resolves which server to connect to, it checks in this order:

1. `--server` flag on the current command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active context in `~/.cratis/config.json`
4. Default: `chronicle://localhost:35000/?disableTls=true`

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

```
* dev    chronicle://localhost:35000/?disableTls=true
  prod   chronicle://prod.example.com:35000/
```

---

### create

Creates a new named context. The first context you create automatically becomes the active context.

```bash
cratis context create <NAME> --server <CONNECTION_STRING>
```

**Options:**

| Option | Short | Description |
|---|---|---|
| `--server` | | Chronicle server connection string |
| `--event-store` | `-e` | Default event store for this context (default: `default`) |
| `--namespace` | `-n` | Default namespace for this context (default: `Default`) |

**Example — local development server with TLS disabled:**

```bash
cratis context create dev --server chronicle://localhost:35000/?disableTls=true
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

Switches to a named context, making it the active context for all subsequent commands.

```bash
cratis context set <NAME>
```

The command validates that the named context exists before activating it. If the name does not match any stored context, it reports an error.

**Example:**

```bash
cratis context set prod
```

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

```
Server:       chronicle://localhost:35000/?disableTls=true
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

```
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
| `management-port` | HTTP management port (default: `8080`) |

**Example — update the server URL:**

```bash
cratis context set-value server chronicle://localhost:35000/?disableTls=true
```

**Example — set credentials for an authenticated server:**

```bash
cratis context set-value client-id my-app
cratis context set-value client-secret s3cr3t
```

The client secret is stored in the config file and masked whenever the CLI displays context details.
