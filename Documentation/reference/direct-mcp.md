---
title: Direct MCP
description: Run the stdio bridge to Direct's MCP server and register it in AI clients without touching unrelated configuration.
---

`cratis direct mcp` is a stdio Model Context Protocol (MCP) server that forwards JSON-RPC to Direct's remote MCP endpoint (`<origin>/mcp`) using the credential stored by [`cratis direct login`](../direct/index.md). The bridge defines no tools of its own; every tool comes from Direct and acts with your identity.

## The bridge

```bash
cratis direct mcp [--url <ORIGIN>] [--tenant <TENANT> | --no-tenant]
```

The origin, tenant and stored credential are resolved once, when the bridge starts. Without options the bridge uses the active Direct login, tenant included. `--url`, `--tenant` and `--no-tenant` select another stored login instead, and once any of them is given the active login's tenant is never inherited: `--url` alone means no tenant, and `--no-tenant` means no tenant on the active origin. If no stored login serves the target, the bridge exits with the `cratis direct login` command to run.

Only JSON-RPC is written to stdout; the target it forwards to and failures are written to stderr. An expiring access token is refreshed before a request, and a token Direct rejects is refreshed once; refreshing rotates the stored refresh token. When the refreshed token is also rejected, the bridge answers the request with an error that asks you to log in again.

Every request gets exactly one answer unless the client cancels it with `notifications/cancelled`, which also stops the request to Direct. When Direct's reply does not contain the answer, for example an HTTP error, an empty or unreadable body, an error without the request's id, or an event stream that ends early, the bridge answers with a JSON-RPC error that keeps Direct's error code and message where there is one. When Direct no longer knows the MCP session (HTTP 404), the request fails with an error asking you to reconnect or restart the server; the client's next `initialize` starts a new session. JSON-RPC batches are answered with an Invalid Request error for each request in them, because MCP clients send one message per line. A single message from Direct larger than 32 MiB is refused rather than held in memory. There is no time limit on a request, since tool calls may run for as long as the client is willing to wait.

## Registering the bridge in AI clients

```bash
cratis direct mcp install [--client <CLIENTS>] [--scope user|project] [--url <ORIGIN>] [--tenant <TENANT> | --no-tenant] [--dry-run]
cratis direct mcp status [--client <CLIENTS>] [--scope user|project]
cratis direct mcp uninstall [--client <CLIENTS>] [--scope user|project] [--dry-run]
```

`install` writes one stdio server entry named `cratis-direct` that runs `cratis direct mcp --url <origin> --tenant <tenant>`, or `cratis direct mcp --url <origin> --no-tenant` when there is no tenant. Both are always pinned, so a later `cratis direct use` does not redirect an existing registration; run `install` again to move it. Without options the origin and tenant are those of the active Direct login. `--url`, `--tenant` and `--no-tenant` choose them the same way they do for the bridge, so `--url` without a tenant option pins no tenant. A stored login for the pinned origin and tenant is required.

`--client` takes a comma-separated list of `claude`, `codex`, `copilot`, `cursor`, `opencode` and `pi`. Without it, `install` registers every client whose configuration file or directory already exists in the scope, `uninstall` removes every registration it recorded in the scope, and `status` reports every client. `--scope user` (the default) writes your own client configuration for every project; `--scope project` writes the current directory's configuration, which is shared with everyone using the repository.

| Client | User scope | Project scope | Member |
|---|---|---|---|
| Claude Code | `~/.claude.json` | `.mcp.json` | `mcpServers.cratis-direct` |
| Codex | `~/.codex/config.toml`, or `$CODEX_HOME/config.toml` | `.codex/config.toml` | `[mcp_servers.cratis-direct]` |
| Copilot in VS Code | VS Code's user `mcp.json`: `~/Library/Application Support/Code/User/` on macOS, `%APPDATA%\Code\User\` on Windows, `$XDG_CONFIG_HOME/Code/User/` (default `~/.config/Code/User/`) on Linux | `.vscode/mcp.json` | `servers.cratis-direct` |
| Cursor | `~/.cursor/mcp.json` | `.cursor/mcp.json` | `mcpServers.cratis-direct` |
| OpenCode | `~/.config/opencode/opencode.json` (or `$XDG_CONFIG_HOME/opencode/`), or an existing `opencode.jsonc` | `opencode.json`, or an existing `opencode.jsonc` | `mcp.cratis-direct` |

Some registrations are reported as unsupported instead of guessed, and nothing is written for them:

- **Pi** has no native MCP configuration file. Load `cratis direct mcp` through a Pi MCP extension.
- **Claude Code with `CLAUDE_CONFIG_DIR` set** keeps its user configuration elsewhere. Use `claude mcp add --scope user` instead.
- **A configuration directory relocated outside your home directory** by `CODEX_HOME`, `XDG_CONFIG_HOME` or `APPDATA` is not written, because ownership is recorded relative to the home directory.
- **Copilot** means the VS Code integration. VS Code Insiders, non-default VS Code profiles and the standalone Copilot CLI are not registered.

`install` fails without changing anything when none of the selected clients can be registered, or when it finds no client configuration and `--client` is not given.

## Ownership and safety

Only the `cratis-direct` member is written or removed. Other servers and settings, formatting, JSONC comments and unrelated TOML stay as they are. Each member written is recorded in `.cratis/direct-mcp.json` in the scope's root: your home directory for the user scope, the project for the project scope. The record is removed once nothing is owned.

- An existing `cratis-direct` entry that is not in the record belongs to you. `install` reports it as a conflict and changes nothing, in any client.
- An owned entry that was changed after it was installed is left alone: `install` and `uninstall` report it as a conflict and change nothing. Restore or remove the entry yourself first.
- An owned entry you removed from the client configuration is treated as absent: `install` adds it again, `uninstall` only forgets it, and `status` reports it as `absent`.
- Running `install` again with the same origin and tenant changes nothing. With a different origin or tenant, it updates only the entries it owns.
- A registration stays where it was written when `CODEX_HOME`, `XDG_CONFIG_HOME`, `APPDATA` or `CLAUDE_CONFIG_DIR` changes later. `uninstall` and `status` act on the recorded file even when the client now reads another location or can no longer be registered; `install` moves an unchanged registration to the client's current location, and leaves it where it is when the client can no longer be registered.
- A record that names a file or member this command could not have written, under any of those settings, is rejected before anything is changed: a path outside the scope's root, or one that does not end in the client's own configuration file.
- `--dry-run` prints each member that would be added, updated or removed, with its exact value in the file's own format (JSON, or the TOML table for Codex), and writes nothing.
- Symbolic links in the configuration path are refused, as are malformed or duplicate-property JSON and TOML shapes that cannot be edited without rewriting your content. A file changed by another process while the command runs is not overwritten; run the command again.

`status` reads only. Per client it reports `registered` (with the command it launches), `modified`, `user-owned`, `absent` or `unsupported`, and the configuration path. For a registration it owns, that is the recorded path, and `registered` also says when the client now reads another file. Clients still apply their own trust and approval prompts, and usually need a restart to load a new server; registration does not claim that a client has connected.

Verified native references: [Claude Code MCP](https://code.claude.com/docs/en/mcp), [VS Code MCP configuration](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration), [Cursor MCP](https://cursor.com/docs/context/mcp), [OpenCode configuration](https://opencode.ai/docs/config/), and [Codex MCP](https://developers.openai.com/codex/mcp/).
