---
title: arc queries list
description: List the query endpoints registered in a running Cratis Arc application with cratis arc queries list, including its options and output columns.
---

Lists registered query endpoint **metadata** from `GET /.cratis/queries` on a running Arc application. It does not execute those queries or return their application data. Use the [Arc discovery workflow](index.md) to confirm prerequisites and the separate application URL first.

## Usage

```bash
cratis arc queries list [--url <URL>] [-o <FORMAT>]
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
| Name | The simple name of the query method (e.g. `AllAccounts`). |
| Namespace | The dot-separated namespace path of the query. |
| Route | The HTTP GET route registered for this query (e.g. `/api/accounts/all-accounts`). |
| Type | The runtime type name of the query (e.g. `ClientObservable`, `IEnumerable`). |
| Summary | The XML documentation summary of the query type, if available. |

## Examples

```bash
# List queries using the default URL
cratis arc queries list

# List queries from a specific URL
cratis arc queries list --url https://myapp.local:5001

# Output as compact JSON
cratis arc queries list -o json-compact
```

A successful response lists names, namespaces, routes, types, and available summaries; JSON returns structured metadata. A successful empty list differs from failed discovery. For an unreachable host or non-success HTTP response, check URL, startup mapping, and access with the application owner. A listed route is not proof that its query succeeds.

## Notes

The introspection endpoint `GET /.cratis/queries` must be registered in the Arc application. This happens automatically when the application calls `MapIntrospectionEndpoints()` during startup.
