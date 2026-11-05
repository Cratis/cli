---
title: Output formats in the Cratis CLI
description: The Cratis CLI supports table, plain, JSON, and compact JSON output through shared options; cratis new uses --format with table, plain, or json.
---

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

List commands produce tab-separated rows with no borders, no color, and no decoration. Column headers appear on the first row.

`chronicle diagnose` instead produces `key=value` scalar lines followed by repeated records with space-separated `key=value` fields. Values containing whitespace, `=`, `"`, or `\` are double-quoted. Within quoted values, `"` becomes `\"` and `\` becomes `\\`. CR and LF become spaces, keeping every record on one line; empty values remain `key=`. Do not split repeated records on spaces without respecting quotes and escapes. For example:

```text
could_not_check="Failed partitions" event_store="My Store" namespace=Default reason="Permission \"denied\""
observer_requirements_satisfied=false
observer_requirement=orders-projection event_store=orders namespace=production status=absent state= reason="not registered in store/namespace"
```

Only with `--require-observer`, diagnose adds `observer_requirements_satisfied` and one `observer_requirement` record per unique exact ID, including satisfied requirements. `status` is `satisfied`, `absent`, `unsatisfied`, or `couldnotcheck`; `state` is empty when no state could be read. JSON output adds `observerRequirementsSatisfied` and `observerRequirements` at the top level, and the array in each scope, with `observerId`, `eventStore`, `namespace`, lowercase `status`, optional `observedState`, and `reason`. Existing `findings` and `checksCouldNotRun` fields keep their meaning. See [Diagnose](../chronicle/diagnose.md#required-observers) for state and exit-code rules.

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

For lists using the shared formatter, the `-q` / `--quiet` flag outputs the selected identifier of each current result, one per line, with no headers or decoration. With an explicit `--output json` or `--output json-compact`, it instead produces a compact JSON array of identifiers. Use it for bounded selection and inspection.

Quiet behavior is command-specific: other commands may suppress output, print a file path, or retain a result object in JSON mode. Check the target command's reference.

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
