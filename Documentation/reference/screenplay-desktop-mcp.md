---
title: 'Screenplay MCP: install in Claude Desktop or ChatGPT Desktop'
description: Connect Claude Desktop or ChatGPT Desktop to local Screenplay models with Cratis CLI, then approve the extension or plugin in your desktop app.
---

Use Cratis CLI to connect Claude Desktop or ChatGPT Desktop to a local Screenplay
model, so you can explore its `.play` files and propose changes from a conversation.
The CLI prepares the package; you still approve and enable it in the desktop app.
MCP (Model Context Protocol) lets the assistant call the local Screenplay server's tools.

## Before you start

Install [Cratis CLI](../getting-started/index.mdx) and a supported desktop app.
Desktop packages include the executable and .NET runtime, so you need neither
Docker nor a .NET SDK to run the server. Choose the
[Claude Desktop](#claude-desktop) or [ChatGPT Desktop](#chatgpt-desktop) steps below.
For ChatGPT, check that your app and account support local Plugins and stdio MCP,
which starts the server as a local process.

macOS arm64/x64 and Windows x64 packages are supported. Linux desktop host support
and Windows arm64 are not claimed; use [Docker/.NET/manual registration](/screenplay/mcp/install/)
for advanced/headless use. Self-contained Linux packages are available for other
compatible local MCP clients.

The CLI acquires packages from **Cratis/Screenplay GitHub releases**, independently
of its own embedded Screenplay library version. Artifacts and `.sha256` files come
only from matching, Cratis-owned HTTPS releases. SHA-256 is verified before
opening/extraction and on every cached reuse. These assets are **unsigned**:
checksums provide integrity, not an independent publisher signature. Only install
trusted packages, and respect device or enterprise policies that prohibit sideloading.

These steps use release packages rather than a public directory listing. Claude
directory distribution requires extension review; OpenAI public local-MCP
publication requires coordination with OpenAI. You can also use
[direct downloads](https://github.com/Cratis/Screenplay/releases).

## Claude Desktop

1. Run the install command for Claude:

   ```bash
   cratis screenplay desktop install --clients claude
   ```

   The CLI verifies the native `.mcpb` extension bundle, then opens it using the
   OS open-file mechanism (on macOS, explicitly in Claude). If Claude is not
   detected, the CLI reports the downloaded bundle's path for manual installation;
   it does not claim to have installed it.

2. Complete Claude's trust/install dialog. If the bundle does not open, go to
   **Settings → Extensions → Advanced settings → Extension Developer → Install
   Extension** and select the downloaded bundle. Update Claude if MCPB is unsupported.

3. Confirm that Screenplay is enabled in **Settings → Extensions**, then name
   your existing model directory in the conversation so the assistant can open it
   with `open-workspace`. Ask it to describe the application in that model. A
   response based on your `.play` files checks the connection; a completed CLI
   command alone does not.

Status records the bundle handed to Claude, **not a verified installation**.
There is no public unattended install/status/uninstall API used here. Confirm the
installed extension and version in **Settings → Extensions**. Update opens the
new bundle and requires confirmation again. Uninstall forgets only the CLI's
handoff receipt; remove the actual extension in Claude's UI. No private Claude
extension storage or project-local Claude Code configuration is read or changed.

## ChatGPT Desktop

1. Choose an existing model directory without symbolic links. Replace the example
   path with its absolute path, quoting it if it contains spaces:

   ```bash
   cratis screenplay desktop install --clients chatgpt --model-root /absolute/path/to/specifications
   ```

   The CLI extracts the plugin into a versioned Cratis-owned directory under
   `~/.codex/plugins/cratis-screenplay/`, then registers a local source in
   `~/.agents/plugins/marketplace.json`. With `--model-root`, the plugin launches
   the server against that directory. This registers a source, not an enabled plugin.

2. Restart ChatGPT Desktop. Open **Plugins**, select the local/Cratis source, then
   install/enable **Screenplay** and confirm tool approval. There is no supported
   unattended enable API used here. If the Plugins UI is absent or local MCP is
   unavailable for your version/account, update the host or use a compatible client.

3. Ask the assistant to describe the application in your model. Check that the
   response reflects your `.play` files before proposing changes. Source
   registration alone is not proof the server is running.

The model directory is optional. Without `--model-root`, a new installation lets
Screenplay select its workspace at runtime from the host's project or a folder
named in the conversation through `open-workspace`. With no project or existing
model in the working directory, it falls back to `Documents/Screenplay` in your
home folder, created on demand. Update retains a previously selected ChatGPT
model directory when `--model-root` is omitted.

The marketplace update preserves its name, unknown properties and the raw contents
of foreign plugin entries. Other entries' array separator whitespace may be
reformatted. The CLI does not edit host-internal caches. The plugin includes
Screenplay instructions and the same local server used by the Docker/.NET
distributions; it introduces no hosted proxy.

Update replaces only an unchanged Cratis-owned entry with a new immutable source.
Reinstalling the same version/root does not rewrite the marketplace or duplicate
packages. Update does not downgrade a newer registered/handed-off version; use
`install --version VERSION` for an intentional downgrade. Uninstall removes only the unchanged owned entry and active fingerprinted
package; **disable/remove the installed plugin in ChatGPT Plugins too**. Host-owned
caches, plugin data, old immutable source versions and verified download caches
are retained. Your selected model is never removed.
If Windows locks a running package or cleanup lacks permission, the source is
unregistered and a **cleanup pending** receipt retains the remaining owned paths.
Disable/remove Screenplay, quit ChatGPT (or restore filesystem access), and rerun
uninstall to finish safely. Install/update is blocked until that cleanup completes.

Once your desktop app can read the model, use [Explore a model](/screenplay/mcp/explore/)
for prompts that trace events and dependencies without changing files.

## Commands

```bash
cratis screenplay desktop install
cratis screenplay desktop install --clients claude,chatgpt --model-root /absolute/path/to/specifications
cratis screenplay desktop status
cratis screenplay desktop update --clients claude,chatgpt
cratis screenplay desktop uninstall --clients chatgpt
```

`install`, `status`, `update`, and `uninstall` manage **user-level desktop
integration**, not project-local AI registrations. `cratis screenplay mcp <verb>`
is deprecated but still works, with a notice on standard error. A bare
`cratis screenplay mcp ROOT` still runs the protocol server. Use `./install`,
`./status`, `./update`, or `./uninstall` for model folders with those reserved names.

| Option | Behavior |
| --- | --- |
| `--clients claude,chatgpt` | Select either or both providers. Noninteractive management requires an explicit selection. |
| `--version VERSION` | Pin a Screenplay semantic release version, without a leading `v`. Otherwise resolve the latest stable Screenplay release. |
| `--model-root DIRECTORY` | Existing physical model folder for the ChatGPT source. Update retains the previous ChatGPT folder when omitted. For Claude, name the folder in the conversation instead. |
| `--dry-run` | Validate and preview install/update/removal without downloading artifacts, writing files, or opening a host. Release metadata may still be queried. |
| `--help` | Show standard CLI help on stdout, for example `cratis screenplay desktop install --help`. |

Interactive management offers a multi-select of detected supported applications.
Application discovery uses standard macOS app locations and Windows application
locations/package discovery. An explicit `--clients` can stage a source even when
detection misses an installation; check the host's Plugins/Extensions capability
before enabling it. Detection alone does not prove a compatible host version or
account entitlement.

## Status, offline operation and safety

Status distinguishes **unsupported**, **not detected**, **not configured**,
**source registered**, **host confirmation required**, and **available update**.
Registered and handed-off versions are not claimed to be host-installed versions.
If a release check fails, status reports local state only. The CLI cannot inspect
host-private version/enablement state.

A missing asset can mean the Screenplay release has not finished publishing.
Wait for its desktop publish jobs rather than substituting an untrusted download.

Downloads are retained under `~/.cratis/mcp-desktop/downloads`. A checksum mismatch
is a hard failure: remove that cached artifact and retry from the official release.
Install/update still contacts the release's checksum source when the package is
cached, so a cached download does not make these commands offline. `--version`
skips latest-version lookup, not checksum verification. For a local-only status
check, supply `--version VERSION`; without it, status checks the latest release.
Uninstall is local/offline.

Ownership receipts live under `~/.cratis/mcp-desktop/` and are separate from
project-owned `.cratis/ai.manifest.json`. Foreign Screenplay entries, malformed JSON,
duplicate properties/entries, symlinked configuration/source paths, edited package
files and changed owned marketplace entries block mutation. There is no `--force`
bypass. Reconcile the conflict explicitly before retrying; do not delete receipts
to trick the installer into treating a foreign entry as owned.

Shared marketplace writes are atomic and recheck the original before replacing it.
Each selected client is processed independently: one failure does not stop the
other provider, but the command returns a nonzero exit code for a partial failure.
Review each client's result before retrying. Keep host approval enabled for `apply`
and `recover-workspace`; installing a source never authorizes model changes.

Maintainers: release Screenplay first, wait for all native packages/checksums, then
release this CLI capability. The [Screenplay maintainer runbook](https://github.com/Cratis/Screenplay/blob/main/Source/DotNET/Screenplay.Mcp/README.md)
documents packaging, signatures, host testing and publisher-owned directory steps.
