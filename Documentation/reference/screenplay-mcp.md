---
title: Screenplay MCP
description: Run the embedded Screenplay MCP server and configure project-local AI host registrations safely.
---

`cratis screenplay mcp` hosts the embedded Screenplay 4.180.1 Model Context Protocol (MCP) server over standard input and output. `cratis version` shows the bundled compiler version; JSON output includes `screenplay.version`. It does not install tools, download packages, or check for CLI updates at startup. Native and Homebrew CLI distributions include the runtime; this command does not require a separate .NET installation.

The server can author and bind every ESM version the bundled Screenplay compiler reads; this does not establish renderability. The CLI's bundled Stage 4.113.0 renderer admits ESM v1 through v20, including event sources, streams and routes, v14 equality query filters on snapshot and observable collection queries, v15 concept generation strategies and v16 stored event names, and renders policy negation, authored XML documentation, non-optional UUID-backed generated concepts as Arc generated-value parameters, scalar and named scalar command responses, and supported event-triggered Automation slices. An event's `id` pin keeps its stored event type name when you rename it. Every render checks existing event type ids before publication; changes or unreadable ids report `CLI-RENDER-016` with `STAGE-EVENT-002`. Observable collections return Chronicle's live read-model stream through Arc; snapshot collections return a one-time list. Projection `all` blocks whose mappings set event-source identities render as Chronicle `FromAll`; literal-mapping or mapping-less blocks still report `STAGE-ESM-017`. PascalCase event-source names render as `{Name}EventSource` with public stream-name constants; colliding constants report `STAGE-ESM-012`. Unsupported generated types (`STAGE-ESM-028`), unsupported generation/response combinations (`STAGE-ESM-038`), mixed reaction effects (`STAGE-ESM-037`), captures, non-event triggers and Translate slices (`STAGE-ESM-024`) remain refused, as do read-model absence assertions (`STAGE-ESM-027`), public events and event-target projections and reducers (`STAGE-ESM-032`), foreign origins and event-source captures (`STAGE-ESM-033`), system-identity reactions that invoke commands whose authorization reads claims or uses opaque policies (`STAGE-ESM-034`) and per-production route overrides (`STAGE-ESM-036`); reaction `runs as` identity and reaction and reducer observer filters render. A specification whose unauthenticated caller carries roles or claims is refused with `STAGE-ESM-011`, including the unchanged canonical negation example. ESM v17 adds `then no events` and named reaction redelivery specifications; unsupported specification shapes report `STAGE-ESM-011`. ESM v19 reaction refusal branches render within Stage's supported subset; shapes it cannot reproduce exactly report `STAGE-ESM-039`. Stage 4.113.0 is built on Screenplay 4.180.1 and admits ESM v18 query-error expectations (`then error` after `when query`): supported query rejection specifications render as companion specifications asserting the validation failure. An optional concept key or filter with a `not empty` rule reports `STAGE-ESM-010`; other unsupported shapes report Stage diagnostics. ESM v20 permits validation rules on generated concepts: the renderer emits both the `New()` factory and the `ConceptValidator`. Specifications using these concepts must pin their generated values; pins failing the rules report `STAGE-ESM-011`. Warning- and information-severity rules on generated concepts report `STAGE-ESM-038`. ESM v21 remains refused by `cratis render` with `CLI-RENDER-004`. See [Screenplay rendering](screenplay.md#executable-model-versions) for command-specific compatibility.

For Claude Desktop and ChatGPT Desktop **installation**, see
[Screenplay desktop MCP](screenplay-desktop-mcp.md). Desktop management uses
`cratis screenplay desktop install|status|update|uninstall`, separately from the protocol
startup below. Bare `install`, `status`, `update`, and `uninstall` remain reserved
for the deprecated desktop route; use `./install`, `./status`, `./update`, or
`./uninstall` to select model folders with those names.

## Command

```bash
cratis screenplay mcp [path]
cratis screenplay mcp --project-root <directory>
cratis screenplay mcp --project-root-env <variable>
```

These are mutually exclusive input modes. The server reads JSON-RPC from stdin and writes only JSON-RPC to stdout. Usage and startup errors go to stderr with a nonzero exit code. `--help` renders the standard CLI help on stdout. Interactive output-format options do not apply.

| Input | Model root |
|---|---|
| Explicit `path` | That directory, resolved physically. It must already exist. |
| No arguments, with `.cratis/ai.json` in the current directory | The configured `mcpServers.screenplay.root`. Without one, the project's model folder, located as for `--project-root`. |
| No arguments, without project configuration | None is fixed. The server binds a workspace on first use: the `path` given to `open-workspace`, then the single workspace root the MCP client offers (re-read when the client changes its roots), then the current directory when it holds a `.screenplay` state folder, or `.play` files directly in it or in a folder directly beneath it. When the client offers several roots it asks for a path. For a single client root the server keeps an existing workspace: the folder nearest the offered root, between it and the model, that already holds `.screenplay` state. Otherwise it serves the project's model folder: the common folder of its `.play` files, else `Source` or `src`, else a `Screenplay` folder at the project root. Starting the server never creates that folder: reads treat a missing folder as an empty model, and the folder is created when the first model document or workspace state is written. With no client root and no such model in the current directory it serves `Documents/Screenplay` in your home folder, which is likewise created only on the first write. Pass a path or configure `.cratis/ai.json` to keep a fixed root. |
| `--project-root <directory>` | The configured root in that exact project. Without a configured root, with or without `.cratis/ai.json`, the CLI serves the project's model folder: `.cratis/screenplay` when it holds `.play` files, else the common folder of the project's `.play` files, else `Source` or `src`, else a `Screenplay` folder at the project root. Starting the server never creates that folder: reads treat a missing folder as an empty model, and the folder is created when the first model document or workspace state is written. A folder between the project and that model which already holds `.screenplay` workspace state (applied identities or an interrupted write) is served instead, the one nearest the project when several do, with a notice on standard error. Startup fails when another of those folders holds an interrupted write. |
| `--project-root-env <variable>` | As above, using an absolute project directory supplied in the named environment variable. Missing or relative values fail. |

With an explicit path or project configuration, the CLI never searches parent projects or substitutes another model. The selected project anchor is resolved physically; symbolic links beneath it in configuration paths or the configured model-root path are rejected. Resolving the selected anchor does not authorize following source-file links.

## Authoring tools

Use `open-workspace` to obtain a revision-bound workspace, then discover the current contract with `syntax-schema`. For whole `.play` documents, use `propose-source`: it parses source server-side and supports creating, replacing, removing, moving, or renaming document keys. For typed edits, use `read-ast` handles and `propose-ast`. Both proposal tools default to Authoring validation; select Executable validation when the proposal must also bind.

`run-specifications` executes specifications through Screenplay's deterministic in-memory reference evaluator. Its optional `proposalId` selects a retained candidate without applying it, including when the proposal's disk base has become stale. It reports passed, failed or unsupported results; it does not run the generated application. Pin `expectedSourceRevision` when continuing a paged result.

Proposals require explicit formatting consent and identity continuity. Parse failures return `SourceParseFailed` with located `authoringDiagnostics` and no proposal. Review a successful proposal with `read-proposal` before `apply`; proposing never writes source files. The older `propose` tool remains executable-only. Source validity, executable readiness, and renderer admission are separate verdicts.

## Model notes

Questions, tasks and review notes live in the committed, server-owned `.screenplay/notes.json` sidecar beneath the model root, outside `.play` syntax and executable revisions. Use the notes proposal/apply workflow rather than editing the format by hand. Do not put secrets or personal data in notes, author names, references or resolutions.

| Tool | Contract |
|---|---|
| `list-model-notes` | Pages notes by anchor, scope, kind, state or reference. The default state is `open`; `view: "findings"` reports unresolved questions as host-only `model-note.open-question` information. Pin `expectedNotesRevision` when continuing pages. |
| `propose-model-notes` | Proposes an atomic add, edit, resolve, reopen, reanchor or remove batch. Requires workspace, catalog and notes revisions; the server supplies note IDs and UTC instants. |
| `read-proposal` with `view: "notes"` | Reviews the before/after notes before `apply`. Pin the proposal's `afterNotesRevision` as `expectedNotesRevision` on continuation. Source proposals also report newly orphaned note IDs in `orphanedNotes`. |

Malformed or unreadable notes never block opening a model. Note tools refuse with `UnreadableNotes` without repairing or rewriting the sidecar; restore known-good content or resolve the conflict before reading notes again. Stable identities keep notes anchored across model renames and moves; removed identities leave visible orphans rather than deleting notes. A resolution records an answer, not an implementation of that answer.

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

Install/update creates a configured `root` directory if needed, but never creates `.play` files. Dry-run creates nothing. A missing configured root is a startup error directing you to run setup or create the directory; without a configured root, startup creates nothing; the `Screenplay` model folder described above appears only on the first write. Uninstall retains the model directory and its contents.

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
