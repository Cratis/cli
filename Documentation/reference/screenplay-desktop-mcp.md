---
title: Screenplay desktop MCP installation
description: Install, update and remove local Screenplay desktop sources for Claude Desktop and ChatGPT Desktop without replacing user-owned configuration.
---

Screenplay runs locally against your `.play` model files. Desktop packages include
the executable and .NET runtime: end users need neither Docker nor a .NET SDK.
The CLI acquires packages from **Cratis/Screenplay GitHub releases**, independently
of its own embedded Screenplay library version.

Prefer a curated directory install once Screenplay's listing is approved. This
release does not claim a public listing: Claude requires extension review, and
OpenAI public local-MCP publication requires coordination with OpenAI. Until then,
use the CLI or [direct downloads](https://github.com/Cratis/Screenplay/releases).

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
| `--model-root DIRECTORY` | Existing physical model folder for the ChatGPT source. Claude asks for its folder in its install dialog. Update retains the previous ChatGPT folder when omitted. |
| `--dry-run` | Validate and preview install/update/removal without downloading artifacts, writing files, or opening a host. Release metadata may still be queried. |
| `--help` | Show standard CLI help on stdout, for example `cratis screenplay desktop install --help`. |

Interactive management offers a multi-select of detected supported applications.
Application discovery uses standard macOS app locations and Windows application
locations/package discovery. An explicit `--clients` can stage a source even when
detection misses an installation; check the host's Plugins/Extensions capability
before enabling it. Detection alone does not prove a compatible host version or
account entitlement.

macOS arm64/x64 and Windows x64 packages are supported. Linux desktop host support
and Windows arm64 are not claimed; use [Docker/.NET/manual registration](/screenplay/mcp/install/)
for advanced/headless use. Self-contained Linux packages are available for other
compatible local MCP clients.

## Claude Desktop

The CLI verifies the native `.mcpb`, then opens it using the public OS open-file
mechanism (on macOS, explicitly in Claude). **Complete the host trust/install
dialog** and choose your existing model directory. If file association is missing,
open **Settings → Extensions → Advanced settings → Extension Developer → Install
Extension** and select the downloaded bundle. Update Claude if MCPB is unsupported.
Enterprise policies can prohibit sideloading; respect them.

Status records the bundle handed to Claude, **not a verified installation**.
There is no public unattended install/status/uninstall API used here. Confirm the
installed extension and version in **Settings → Extensions**. Update opens the
new bundle and requires confirmation again. Uninstall forgets only the CLI's
handoff receipt; remove the actual extension in Claude's UI. No private Claude
extension storage or project-local Claude Code configuration is read or changed.

## ChatGPT Desktop

The CLI extracts the plugin into a versioned Cratis-owned directory under
`~/.codex/plugins/cratis-screenplay/`, then registers a local source in
`~/.agents/plugins/marketplace.json`. It preserves the marketplace name, unknown
properties and the raw contents of foreign plugin entries. Other entries' array
separator whitespace may be reformatted. It does not edit host-internal caches.

Restart an up-to-date ChatGPT Desktop that supports local Plugins and stdio MCP.
Open **Plugins**, select the local/Cratis source, then install/enable **Screenplay**.
There is no supported unattended enable API used here. If the Plugins UI is absent
or local MCP is unavailable for your version/account, update the host or use a
compatible client; source registration alone is not proof the server is running.

With `--model-root`, the owned plugin launches the selected physical folder. Without
it, a new install uses the host's persistent plugin data directory and creates an
empty `model` application. The plugin includes Screenplay instructions and the same
local server used by the Docker/.NET distributions. No hosted proxy is introduced.

Update replaces only an unchanged Cratis-owned entry with a new immutable source.
Reinstalling the same version/root does not rewrite the marketplace or duplicate
packages. Update does not downgrade a newer registered/handed-off version; use
`install --version VERSION` for an intentional downgrade. Uninstall removes only the unchanged owned entry and active fingerprinted
package; **disable/remove the installed plugin in ChatGPT Plugins too**. Host-owned
caches, plugin data (including the default model), old immutable source versions and
verified download caches are retained. Your selected model is never removed.
If Windows locks a running package or cleanup lacks permission, the source is
unregistered and a **cleanup pending** receipt retains the remaining owned paths.
Disable/remove Screenplay, quit ChatGPT (or restore filesystem access), and rerun
uninstall to finish safely. Install/update is blocked until that cleanup completes.

## Status, offline operation and safety

Status distinguishes **unsupported**, **not detected**, **not configured**,
**source registered**, **host confirmation required**, and **available update**.
Registered and handed-off versions are not claimed to be host-installed versions.
If a release check fails, status reports local state only. The CLI cannot inspect
host-private version/enablement state.

Artifacts and `.sha256` files are downloaded only from matching, Cratis-owned
HTTPS releases. SHA-256 is verified before opening/extraction and on every cached
reuse. These assets are currently **unsigned**: checksums provide integrity, not
an independent publisher signature. Only install trusted packages. A missing asset
can mean the upstream release has not finished publishing; wait for its desktop
publish jobs rather than substituting an untrusted download.

Downloads are retained under `~/.cratis/mcp-desktop/downloads`. A checksum mismatch
is a hard failure: remove that cached artifact and retry from the official release.
Offline install/update needs both release metadata and the checksum source; even a
cached package is not silently trusted without verification. `--version` skips
latest-version lookup, not checksum verification. Uninstall is local/offline.

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
