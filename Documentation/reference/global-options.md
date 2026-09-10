# Global Options

Every `cratis` command accepts the following options regardless of which subcommand you run.

## --output / -o

Controls the format of command output.

```bash
cratis <command> -o <FORMAT>
```

| Value | Description |
|---|---|
| `table` | Rich terminal table with borders and color. Default in interactive terminals. |
| `plain` | Tab-separated rows, no decoration. Suitable for shell scripting and parsing. |
| `json` | Pretty-printed JSON with indentation. |
| `json-compact` | Compact single-line JSON. May be selected by a recognized tool-environment marker. |

In interactive terminals the default is `table`. Redirected output, `NO_COLOR`, and recognized tool-environment markers can select another current default. Use `--output` explicitly for automation and bind parsing to the exact CLI version; ordinary output formats are not an unversioned stable machine contract.

**Example:**

```bash
cratis chronicle event-types list -o plain
```

---

## --quiet / -q

Outputs only the key identifier for each result, one per line, with no headers or decoration.

```bash
cratis chronicle observers list -q
```

This mode prints identifiers for bounded selection or inspection:

```bash
cratis chronicle observers list -q | head -n 5
```

Review the target command, event store, namespace, and operational procedure before using an identifier as input to a state-changing command.

---

## --yes / -y

Skips confirmation prompts on state-changing commands such as replay, retry, and remove.

```bash
cratis chronicle observers replay <ID> -y
```

State-changing commands do not proceed in automation, redirected output, or other non-interactive environments unless this flag is supplied. Use it only when the exact target, authorization, current state, and recovery procedure are already bounded; the prompt exists to prevent accidental changes.

Set `CRATIS_NONINTERACTIVE=1` when an automation host uses a terminal-like input/output stream but must never be prompted:

```bash
CRATIS_NONINTERACTIVE=1 cratis chronicle observers replay <ID> -y
```

Without `--yes`, the command fails with a nonzero validation exit code.

---

## --debug

Prints diagnostic information to stderr before executing the command. No server output is affected.

The debug panel includes:

- Config file path
- Active context name
- Connection string (credentials are redacted)
- Resolved output format
- Resolved event store and namespace, each with the source it came from (option, context, or built-in default)
- RPC timing for each gRPC call

```bash
cratis chronicle observers list --debug
```

This flag is useful for diagnosing connection problems, verifying which context is active, and measuring server response times.

---

## NO_COLOR Environment Variable

Setting `NO_COLOR` to any value disables ANSI color codes and falls back to plain output:

```bash
NO_COLOR=1 cratis chronicle event-types list
```

This follows the [no-color.org](https://no-color.org) convention and is respected by all output formats.
