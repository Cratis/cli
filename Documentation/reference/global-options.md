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
| `json-compact` | Compact single-line JSON. Default in AI environments. |

In interactive terminals the default is `table`. In recognized agent processes such as Claude Code, Cursor, Windsurf, and Pi, the default is `json-compact` to minimize token consumption.

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

This is the most compact output mode. It is designed for piping — the identifiers it prints can be passed directly to other commands:

```bash
cratis chronicle observers list -q | xargs -I {} cratis chronicle observers replay {} -y
```

---

## --yes / -y

Skips confirmation prompts on destructive commands such as replay, retry, and remove.

```bash
cratis chronicle observers replay <ID> -y
```

Destructive commands do not proceed in automation, redirected output, or other non-interactive environments unless this flag is supplied. Do not use it as a habit when running commands manually — the prompt exists to prevent accidents.

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
