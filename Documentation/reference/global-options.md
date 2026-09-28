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

`cratis llm-context` marks every command that asks for confirmation with `"requiresConfirmation": true`, next to its `effect`. See [LLM Context](llm-context.md).

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

---

## Update hints and CRATIS_NO_UPDATE_CHECK

After a command finishes in an interactive terminal, the CLI may print a one-line hint:

- `↑ Update available: <current> -> <latest>` when a newer CLI is published where this installation updates from (NuGet for the dotnet tool, the GitHub releases for native installations).
- `↑ Stage image update available: <version>` when a Stage image is already on this computer and a newer one exists.
- `↑ Cratis AI update available: <n> new commits since <installed> - run 'cratis ai update'` when the current directory has [Cratis AI](../ai/index.md) installed and the default branch of Cratis/AI has moved past the installed commit.

The checks run in the background while the command runs. Once it is done, the CLI waits a fraction of a second in total for all of them, and a check that has not answered by then shows nothing this time.

Answers are cached in `~/.cratis/version-check.json`. A cached answer less than an hour old is used without asking again. After that the source is asked again: if it is slow, an update found within the last day is shown meanwhile, and the new answer is recorded if it arrives before the CLI exits. A failed request is not repeated for 15 minutes, or for an hour when GitHub reports its rate limit as spent. No hint is printed for shell completion, redirected output, or a detected AI agent environment.

The cache file is shared by every `cratis` process without a lock. Two processes that finish at the same moment can lose one of their cache entries, and on Windows an update that fails because another process holds the file open is dropped the same way. Either only costs one extra request on a later run.

Set `CRATIS_NO_UPDATE_CHECK` to any value to switch all of these checks off, including the local Docker lookup behind the Stage image hint:

```bash
CRATIS_NO_UPDATE_CHECK=1 cratis chronicle event-types list
```
