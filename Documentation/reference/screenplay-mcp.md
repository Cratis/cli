---
title: Screenplay MCP
description: Run the embedded Screenplay MCP server and configure project-local AI host registrations safely.
---

`cratis screenplay mcp` hosts the embedded Screenplay Model Context Protocol (MCP) server over standard input and output. It does not install tools, download packages, or check for CLI updates at startup. Native and Homebrew CLI distributions include the runtime; this command does not require a separate .NET installation.

## Command

```bash
cratis screenplay mcp [path]
cratis screenplay mcp --project-root <directory>
cratis screenplay mcp --project-root-env <variable>
```

These are mutually exclusive input modes. The server reads JSON-RPC from stdin and writes only JSON-RPC to stdout. Usage and startup errors go to stderr with a nonzero exit code. `--help` writes usage to stderr. Interactive output-format options do not apply.

| Input | Model root |
|---|---|
| Explicit `path` | That directory, resolved physically. It must already exist. |
| No arguments, with `.cratis/ai.json` in the current directory | The project's configured Screenplay root. |
| No arguments, without project configuration | The current directory. |
| `--project-root <directory>` | The configured root in that exact project; `.cratis/ai.json` is required. |
| `--project-root-env <variable>` | As above, using an absolute project directory supplied in the named environment variable. Missing or relative values fail. |

The CLI never searches parent projects or silently substitutes another model. The selected project anchor is resolved physically; symbolic links beneath it in configuration paths or the configured model-root path are rejected. Resolving the selected anchor does not authorize following source-file links.

## Automatic project registration

[Cratis AI installation](../ai/index.md) registers the server when the corpus contains `.cratis/ai/mcp-servers.json` and the expanded selected profile composition includes `cratis/screenplay`. Composed profiles such as Stage can therefore select Screenplay without naming it separately. An older corpus without the descriptor continues to install normally, without MCP registrations.

The corpus descriptor supplies the trusted command, arguments, and default root. Installation renders those values; it never executes the server. The Screenplay default is `.cratis/screenplay`.

The optional project-owned `mcpServers` property in `.cratis/ai.json` overrides the model directory or disables registration:

```json
{
  "schemaVersion": "1.0",
  "harnesses": ["claude", "copilot", "cursor", "opencode"],
  "profiles": ["cratis/screenplay"],
  "languages": [],
  "mcpServers": {
    "screenplay": {
      "enabled": true,
      "root": ".cratis/screenplay"
    }
  }
}
```

`enabled` defaults to `true` for matching profiles. `root` defaults to the corpus descriptor's `defaultRoot`; startup uses `.cratis/screenplay` if no descriptor is installed. A root override must be project-relative, without `..`, host placeholders, or symbolic links. An explicit `.` selects the entire project as one model. Do not use it for unrelated applications.

Install/update creates the selected directory if needed, but never creates `.play` files. Dry-run creates nothing. Server startup never creates directories: a missing configured root is an error directing you to run setup or create the directory. Uninstall retains the model directory and its contents.

## Native harness configuration

Only selected harnesses receive entries. Model-root overrides remain in `.cratis/ai.json`; generated host files contain no developer-specific absolute paths.

| Harness | Native project file | Project anchor |
|---|---|---|
| Claude Code | `.mcp.json`, `mcpServers.screenplay` | `--project-root-env CLAUDE_PROJECT_DIR`; requires a host that supplies this environment variable. |
| Copilot in VS Code | `.vscode/mcp.json`, `servers.screenplay` | `--project-root ${workspaceFolder}`. This is the VS Code integration, not the standalone Copilot CLI. |
| Cursor | `.cursor/mcp.json`, `mcpServers.screenplay` | `--project-root ${workspaceFolder}`. |
| OpenCode | `opencode.json`, or an existing `opencode.jsonc`, `mcp.screenplay` | `--project-root .`; OpenCode starts local transports in its instance directory. Start it at the project root containing `.cratis/ai.json`. |
| Codex | `.codex/config.toml`, `[mcp_servers.screenplay]` | `--project-root .`, using Codex's native runtime working directory. Launch Codex at the project root containing `.cratis/ai.json`; only trusted project configuration is loaded. |
| Pi | Native `cratis-mcp` extension through `.pi/extensions/` | Reads the installed descriptor, expanded profile catalog, and project root setting. No separate bridge installation or invented MCP config file. |

Pi requires a corpus carrying the native `cratis-mcp` extension. Install/update/status report a missing extension or unknown adapter explicitly in `unsupportedMcpServers`, rather than claiming MCP is configured. Status also lists native server entries and `mcpExtensions`. If both OpenCode configuration filenames exist, installation refuses to guess which owns the entry.

Native host trust, approval, and restart requirements still apply. Registration does not bypass them or claim that a host connected successfully.

Verified native references: [Claude Code MCP](https://code.claude.com/docs/en/mcp), [VS Code MCP configuration](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration), [Cursor MCP](https://cursor.com/docs/context/mcp), [OpenCode MCP](https://opencode.ai/docs/mcp-servers/), and [Codex MCP](https://developers.openai.com/codex/mcp/).

## Ownership and preservation

Host configuration files are not replaced by symbolic links. The installer edits only the owned server member using JSON token spans or parsed TOML spans, retaining unrelated bytes, whitespace, comments, and input definitions. JSONC comments and trailing commas are supported. Malformed syntax, duplicate properties, and symlinked configuration paths are rejected before installation writes.

Codex entries use ordinary named TOML tables. If an existing inline parent table cannot be extended without changing user content, installation fails before writing; normalize that table explicitly before retrying. A foreign Screenplay table, including quoted or inline variants, is always a conflict. Global `~/.codex/config.toml` is never modified.

`.cratis/ai.manifest.json` records each installed member and its preimage. An existing foreign `screenplay` entry is a conflict even if its value happens to match. A changed owned entry is drift. Neither is overwritten by `--force`.

Update removes an unchanged owned entry when its profile/harness is deselected or it is disabled. Uninstall removes only unchanged owned entries and leaves other servers and properties intact. Changed entries block removal so the manifest remains available for reconciliation. Status is read-only; dry-run plans the same member operations without writing. Each shared configuration update is atomic, and a detected concurrent edit aborts rather than overwriting it.
