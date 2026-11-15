---
description: Use cratis arc to discover a running Arc application's registered command and query routes, choose its URL, and check discovery access requirements.
---

# Arc

`cratis arc` lists the commands and queries registered by a running Cratis Arc application, including their HTTP routes. Use it when you need to find an endpoint without searching the application's source. It reads the application's discovery catalogs, sometimes called introspection endpoints, rather than connecting to Chronicle. Arc does not require Chronicle for this workflow.

## How do I find the application's routes?

[Install the CLI](/cli/getting-started/#install-it), start your Arc application, and confirm its base URL. The application must expose its command and query catalogs to this client. These commands read those catalogs; they do not execute application commands or queries:

```bash
cratis arc commands list --url http://localhost:5000
cratis arc queries list --url http://localhost:5000
```

Replace the URL with the one your application is listening on. The command list includes names, namespaces, routes, and documentation summaries; the query list also shows the query type. Use the reported route to locate the endpoint you need, then follow the application's API contract to call it.

The CLI reads `/.cratis/commands` and `/.cratis/queries`. In Arc's C# host, discovery defaults to anonymous access in Development and authenticated access elsewhere. Outside Development, a host without an authentication mechanism does not map discovery by default. Configuration can also disable catalogs or require roles. See [Arc introspection](/arc/backend/csharp/introspection/) for exposure and access settings.

The CLI's Arc HTTP client does not attach authentication credentials and skips TLS certificate validation. Use it only against an application you trust on a trusted local connection. A reachable URL alone does not mean its catalogs are available to this client. If discovery fails, check the application's catalog configuration and access requirements; do not make protected catalogs public just to run an inspection command.

## Connection Flags

All `cratis arc` commands accept the following connection flag:

| Flag | Description |
|---|---|
| `--url <URL>` | Base URL of the Arc application. Overrides the `ARC_URL` environment variable and launch settings. With no configured URL, falls back to `http://localhost:5000`. |

## Connection Resolution Order

The CLI resolves the Arc application URL in this order:

1. `--url` flag
2. `ARC_URL` environment variable
3. `Properties/launchSettings.json` or `properties/launchSettings.json` `applicationUrl`
4. Default: `http://localhost:5000`

Launch settings are searched from the current directory through its parents. The CLI uses the first URL from the first profile with a non-empty `applicationUrl`. Pass `--url` when that choice is not the application you want to inspect. Chronicle contexts and `--server` do not select the Arc application.

## Sub-Commands

| Sub-command | Description |
|---|---|
| `commands` | List registered command endpoints in the Arc application. |
| `queries` | List registered query endpoints in the Arc application. |

## Global Flags

All `cratis arc` commands also inherit the top-level global flags:

| Flag | Description |
|---|---|
| `-o, --output <FORMAT>` | Output format: `table`, `plain`, `json`, `json-compact`. |
| `-q, --quiet` | Output only key identifiers, one per line. |
| `--debug` | Print debug information to stderr. |

For a script, select an output format explicitly and bind parsing to the CLI version you use. These output formats are not an unversioned stable machine contract. To create an application instead of inspecting one, continue with [project creation](/cli/new/).
