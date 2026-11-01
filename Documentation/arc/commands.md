---
title: arc commands list
description: List the command endpoints registered in a running Cratis Arc application with cratis arc commands list, including its options and output columns.
---

Lists registered command endpoint **metadata** from `GET /.cratis/commands` on a running Arc application. It does not invoke those commands or verify their behavior. Use the [Arc discovery workflow](index.md) to confirm prerequisites and the separate application URL first.

## Usage

```bash
cratis arc commands list [--url <URL>] [-o <FORMAT>]
```

## Options

| Option | Description |
|---|---|
| `--url <URL>` | Base URL of the Arc application (default: `http://localhost:5000`). |
| `-o, --output <FORMAT>` | Output format: `table`, `plain`, `json`, `json-compact`. |

## Output

Each row contains:

| Column | Description |
|---|---|
| Name | The simple type name of the command (e.g. `OpenDebitAccount`). |
| Namespace | The dot-separated namespace path of the command. |
| Route | The HTTP POST route registered for this command (e.g. `/api/accounts/open-debit-account`). |
| Summary | The XML documentation summary of the command type, if available. |

## Examples

```bash
# List commands using the default URL
cratis arc commands list

# List commands from a specific URL
cratis arc commands list --url https://myapp.local:5001

# Output as compact JSON
cratis arc commands list -o json-compact
```

A successful response lists names, namespaces, routes, and available summaries; JSON returns structured metadata. A successful empty list is not a connection failure. Non-success HTTP responses return a server error, while unreachable hosts fail discovery. Check URL, startup mapping, and access with the application owner rather than inferring that no commands exist.

## Notes

The introspection endpoint `GET /.cratis/commands` must be registered in the Arc application. This happens automatically when the application calls `MapIntrospectionEndpoints()` during startup.
