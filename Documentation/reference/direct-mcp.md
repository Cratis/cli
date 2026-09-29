---
title: Direct MCP
description: Run the stdio bridge to Direct's MCP server and register it in AI clients without touching unrelated configuration.
---

`cratis direct mcp` is a stdio Model Context Protocol (MCP) server that forwards JSON-RPC to Direct's remote MCP endpoint (`<origin>/mcp`) using the credential stored by [`cratis direct login`](../direct/index.md). The bridge defines no tools of its own; every tool comes from Direct and acts with your identity.

## The bridge

```bash
cratis direct mcp [--url <ORIGIN>] [--tenant <TENANT>]
```

The origin, tenant and stored credential are resolved once, when the bridge starts. Without options the bridge uses the active Direct login; `--url` and `--tenant` select another stored login instead. A tenant is inherited from the active login only on the active origin. If no stored login serves the target, the bridge exits with the `cratis direct login` command to run.

Only JSON-RPC is written to stdout; the target it forwards to and failures are written to stderr. An expiring access token is refreshed before a request, and a token Direct rejects is refreshed once; refreshing rotates the stored refresh token. When the refreshed token is also rejected, the bridge answers the request with an error that asks you to log in again.

## Registering the bridge in AI clients

```bash
cratis direct mcp install [--client <CLIENTS>] [--scope user|project] [--url <ORIGIN>] [--tenant <TENANT>] [--dry-run]
cratis direct mcp status [--client <CLIENTS>] [--scope user|project]
cratis direct mcp uninstall [--client <CLIENTS>] [--scope user|project] [--dry-run]
```

`install` writes one stdio server entry named `cratis-direct` that runs `cratis direct mcp --url <origin> [--tenant <tenant>]`. The origin and tenant are those of the active Direct login, or of `--url`/`--tenant`, and a stored login for them is required. Pinning them means a later `cratis direct use` does not redirect an existing registration; run `install` again to move it. When the login has no tenant, only the origin is pinned.

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
- Running `install` again with the same origin and tenant changes nothing. With a different origin or tenant, it updates only the entries it owns.
- A record that names a file or member this command could not have written is rejected before anything is changed.
- `--dry-run` prints each member that would be added, updated or removed, with its exact value in the file's own format (JSON, or the TOML table for Codex), and writes nothing.
- Symbolic links in the configuration path are refused, as are malformed or duplicate-property JSON and TOML shapes that cannot be edited without rewriting your content. A file changed by another process while the command runs is not overwritten; run the command again.

`status` reads only. Per client it reports `registered` (with the command it launches), `modified`, `user-owned`, `absent` or `unsupported`, and the configuration path. Clients still apply their own trust and approval prompts, and usually need a restart to load a new server; registration does not claim that a client has connected.

Verified native references: [Claude Code MCP](https://code.claude.com/docs/en/mcp), [VS Code MCP configuration](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration), [Cursor MCP](https://cursor.com/docs/context/mcp), [OpenCode configuration](https://opencode.ai/docs/config/), and [Codex MCP](https://developers.openai.com/codex/mcp/).
