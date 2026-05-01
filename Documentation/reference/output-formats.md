# Output Formats

The `cratis` CLI supports four output formats controlled by the `-o` / `--output` flag. Choosing the right format for your context dramatically reduces token consumption and improves scripting reliability.

## Formats

### table

Rich terminal output with borders, column alignment, and ANSI colors. This is the default when running interactively in a terminal.

```bash
cratis event-types list -o table
```

Use `table` when you are reading output yourself and want it to be easy to scan. Do not use it in scripts or AI prompts — it produces the most tokens of any format.

---

### plain

Tab-separated rows with no borders, no color, and no decoration. Column headers appear on the first row.

```bash
cratis event-types list -o plain
```

Use `plain` when:

- Piping output to `awk`, `grep`, `cut`, or similar tools.
- Writing shell scripts that parse individual fields.
- Sending output to an AI assistant where token count matters but you do not need structured data.

`plain` is roughly 34 times smaller than `json` for event-types list output, 25 times smaller for events, and 27 times smaller for read-models list output. These are significant savings when working with large event stores.

---

### json

Pretty-printed JSON with indentation and newlines.

```bash
cratis event-types list -o json
```

Use `json` when:

- You need structured, machine-readable data.
- You are writing a tool that calls `cratis` as a subprocess and parses its output with a JSON library.
- Human readability of the JSON matters (for debugging, for example).

---

### json-compact

Compact JSON with no extra whitespace. This is the default in AI assistant environments (Claude Code, Cursor, Windsurf).

```bash
cratis event-types list -o json-compact
```

Use `json-compact` when:

- You need structured data in an AI prompt or context window.
- You are piping JSON to another tool that does not care about formatting.
- You want to minimize token usage while still having parseable structure.

`json-compact` is smaller than `json` and produces structured data, making it the best choice when an AI tool needs to interpret results.

---

## The --quiet Flag

The `-q` / `--quiet` flag outputs only the primary identifier of each result, one per line, with no headers and no decoration. It is even more compact than `plain` and is designed specifically for piping identifiers into other commands.

```bash
cratis observers list -q
```

**Piping example — replay all observers:**

```bash
cratis observers list -q | xargs -I {} cratis observers replay {} -y
```

This pattern works for any command that accepts an identifier argument. Use `--quiet` on the listing command to produce clean input for the action command.

---

## Format Selection Summary

| Situation | Recommended format |
|---|---|
| Reading output in a terminal | `table` (default) |
| Shell scripting and parsing | `plain` |
| Structured data with human readability | `json` |
| AI assistants and token-sensitive contexts | `json-compact` (default in AI environments) |
| Piping identifiers to another command | `--quiet` |
