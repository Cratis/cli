---
title: Screenplay MCP
description: Run the embedded Screenplay MCP server and configure project-local AI host registrations safely.
---

`cratis screenplay mcp` hosts the embedded Screenplay 4.74.0 Model Context Protocol (MCP) server over standard input and output. `cratis version` shows the bundled compiler version; JSON output includes `screenplay.version`. It does not install tools, download packages, or check for CLI updates at startup. Native and Homebrew CLI distributions include the runtime; this command does not require a separate .NET installation.

For Claude Desktop and ChatGPT Desktop **installation**, see
[Screenplay desktop MCP](screenplay-desktop-mcp.md). Desktop management uses
`cratis screenplay mcp install|status|update|uninstall`, separately from the protocol
startup below. Use `./install`, `./status`, `./update`, or `./uninstall` for model
folders with those reserved names.

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
| No arguments, with `.cratis/ai.json` in the current directory | The configured `mcpServers.screenplay.root`. Without one, the project's model folder, located as for `--project-root`. |
| No arguments, without project configuration | None is fixed. The server binds a workspace on first use: the `path` given to `open-workspace`, then the single workspace root the MCP client offers (re-read when the client changes its roots), then the current directory when it holds a `.screenplay` state folder, or `.play` files directly in it or in a folder directly beneath it. When the client offers several roots it asks for a path. For a single client root the server keeps an existing workspace: the folder nearest the offered root, between it and the model, that already holds `.screenplay` state. Otherwise it serves the project's model folder: the common folder of its `.play` files, else `Source` or `src`, else a new `Screenplay` folder it creates. With no client root and no such model in the current directory it creates and serves `Documents/Screenplay` in your home folder. Pass a path or configure `.cratis/ai.json` to keep a fixed root. |
| `--project-root <directory>` | The configured root in that exact project. Without a configured root, with or without `.cratis/ai.json`, the CLI serves the project's model folder: `.cratis/screenplay` when it holds `.play` files, else the common folder of the project's `.play` files, else `Source` or `src`, else a new `Screenplay` folder it creates. |
| `--project-root-env <variable>` | As above, using an absolute project directory supplied in the named environment variable. Missing or relative values fail. |

With an explicit path or project configuration, the CLI never searches parent projects or substitutes another model. The selected project anchor is resolved physically; symbolic links beneath it in configuration paths or the configured model-root path are rejected. Resolving the selected anchor does not authorize following source-file links.

## Event model board

The bundled server includes the `visualize-model` MCP App. In a host that renders MCP Apps, it draws your Screenplay application as an event model board beside the conversation. You can also pass a `proposalId` to inspect a proposed change before applying it, or `sketch` documents to visualize a what-if without writing files.

The host must advertise the `io.modelcontextprotocol/ui` extension in its `initialize` capabilities. If it supplies `mimeTypes`, they must include `text/html;profile=mcp-app`. The server then lists `visualize-model` in `tools/list` and serves the board at `ui://screenplay/event-model-board.html` through MCP resources. No separate HTTP server is needed.

A host without that capability still receives the authoring tools, but does not receive `visualize-model` or the board resource. Installing a server registration alone does not enable visual rendering in a host.

## Automatic project registration

[Cratis AI installation](../ai/index.md) registers the server when the corpus contains `.cratis/ai/mcp-servers.json` and the expanded selected profile composition includes `cratis/screenplay`. Composed profiles such as Stage can therefore select Screenplay without naming it separately. An older corpus without the descriptor continues to install normally, without MCP registrations.

The corpus descriptor supplies the trusted command and arguments. Installation renders those values; it never executes the server.

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

`enabled` defaults to `true` for matching profiles. Without `root`, the server locates the project's model folder at startup, as described for `--project-root`. A root override must be project-relative, without `..`, host placeholders, or symbolic links. An explicit `.` selects the entire project as one model. Do not use it for unrelated applications.

Install/update creates a configured `root` directory if needed, but never creates `.play` files. Dry-run creates nothing. A missing configured root is a startup error directing you to run setup or create the directory; without a configured root, startup may create the `Screenplay` model folder described above. Uninstall retains the model directory and its contents.

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
