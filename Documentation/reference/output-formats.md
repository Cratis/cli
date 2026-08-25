# Output Formats

The `cratis` CLI currently provides four output formats controlled by the `-o` / `--output` flag. Select the format that matches the exact human or tool workflow, and bind automation to the CLI version being used.

## Formats

### table

Rich terminal output with borders, column alignment, and ANSI colors. This is the default when running interactively in a terminal.

```bash
cratis chronicle event-types list -o table
```

Use `table` when you are reading output yourself and want terminal-oriented formatting. Select `plain` or a JSON format explicitly when a tool needs a different current representation.

---

### plain

Tab-separated rows with no borders, no color, and no decoration. Column headers appear on the first row.

```bash
cratis chronicle event-types list -o plain
```

Use `plain` when:

- Piping output to `awk`, `grep`, `cut`, or similar tools.
- Writing shell scripts that parse individual fields.
- Sending output to an AI assistant where token count matters but you do not need structured data.

Plain output avoids repeated JSON field names and formatting, but its size depends on the command and returned data. Measure the exact version and workload before making a size or token assumption.

---

### json

Pretty-printed JSON with indentation and newlines.

```bash
cratis chronicle event-types list -o json
```

Use `json` when:

- You need structured, machine-readable data.
- You are writing a tool that calls `cratis` as a subprocess and parses its output with a JSON library.
- Human readability of the JSON matters (for debugging, for example).

---

### json-compact

Compact JSON with no extra whitespace. A recognized tool-environment marker may select it as the current default.

```bash
cratis chronicle event-types list -o json-compact
```

Use `json-compact` when:

- You need structured data in an AI prompt or context window.
- You are piping JSON to another tool that does not care about formatting.
- You want to minimize token usage while still having parseable structure.

`json-compact` removes pretty-printing whitespace while retaining the current named JSON structure. Use `--output json-compact` explicitly and review the exact CLI version before relying on that structure.

---

## The --quiet Flag

The `-q` / `--quiet` flag outputs the primary identifier of each current result, one per line, with no headers or decoration. Use it for bounded selection and inspection.

```bash
cratis chronicle observers list -q
```

**Bounded read-only example:**

```bash
cratis chronicle observers list -q | head -n 5
```

Before passing an identifier to a state-changing command, confirm the event store, namespace, target, authorization, current state, and documented operational procedure.

---

## Format Selection Summary

| Situation | Recommended format |
|---|---|
| Reading output in a terminal | `table` (default) |
| Shell scripting and parsing | `plain` |
| Structured data with human readability | `json` |
| AI assistants and token-sensitive contexts | `json-compact` (default in AI environments) |
| Piping identifiers to another command | `--quiet` |
