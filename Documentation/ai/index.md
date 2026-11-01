---
description: Install and maintain Cratis rules, skills, agents, prompts and hooks in a repository with cratis ai, one shared copy for every coding agent.
---

# Cratis AI

`cratis ai` installs and maintains Cratis-owned AI guidance — rules, skills, agents, prompts, hooks, and harness integration — in a repository. The content comes from the [Cratis AI](https://github.com/Cratis/AI) corpus.

The idea is one shared corpus and many thin adapters. Guidance is installed once, into `.cratis/ai/`, and every coding agent you use is pointed at that same copy. Claude Code, Codex, Copilot, Cursor, OpenCode and pi each read guidance from their own conventional location, so each gets a native adapter that links back to the one corpus. Nothing is duplicated per tool, and updating the corpus updates every tool at once.

## Commands

```bash
cratis ai install         # choose what this repository needs, and install it
cratis ai update          # re-synchronize the choice already recorded
cratis ai status          # what is configured, installed, available, and locally modified
cratis ai profiles list   # every profile the corpus offers
cratis ai uninstall       # remove unchanged Cratis-managed content
cratis ai test            # opt-in: run conformance fixtures against installed AI harnesses
```

See [Test AI conformance](test.md) for `cratis ai test`.

Run `install` once per repository. Use `update` from then on.

## `cratis ai install`

Installs the corpus into the current repository and records your selection in `.cratis/ai.json`.

### What it writes

```text
.cratis/
├── ai.json                  your selection — commit this
├── ai.manifest.json         what Cratis installed, with hashes — commit this
└── ai/
    ├── rules/               always-on and path-scoped rules
    ├── skills/              on-demand skills
    ├── agents/              subagent definitions
    ├── prompts/             slash commands and prompts
    ├── hooks/               enforcement scripts
    └── harnesses/<name>/    harness-specific content, e.g. pi extensions, Cursor rules

AGENTS.md, CLAUDE.md        links to the project instructions
.claude/ .cursor/ .github/  native adapters, each linking into .cratis/ai/
.opencode/ .pi/ .agents/
```

Guidance adapters are symbolic links into `.cratis/ai/`, not copies. Copilot's `.github/instructions` links to `.cratis/ai/harnesses/copilot/instructions` when the selected corpus supplies native `*.instructions.md` files; older corpora fall back to `.cratis/ai/rules`. In the same way, pi's `.pi/agents` links to `.cratis/ai/harnesses/pi/agents` when the corpus supplies pi-native agent adapters, whose tool names any pi agent loader understands; older corpora fall back to the canonical `.cratis/ai/agents`, whose Claude-style tool names only Cratis' own pi subagent loader translates. OpenCode's `.opencode/agents` follows the same pattern with `.cratis/ai/harnesses/opencode/agents`. `cratis ai update` relinks an unchanged Cratis-owned link when its target changes. A user-owned file already sitting at one of those paths is left exactly as it is — install never replaces your own content.

Selected profiles can also supply MCP servers. [Screenplay MCP](../reference/screenplay-mcp.md) uses native project configuration entries, **not symlinks**: unrelated server definitions, comments, and settings remain intact. Every server in the corpus `mcp-servers.json` whose profiles intersect the selected profiles is registered in each managed harness configuration (`.mcp.json`, `.vscode/mcp.json`, `.cursor/mcp.json`, `.codex/config.toml`, OpenCode), using the descriptor's `command`, `args` and optional `env` exactly as declared; deselecting a profile or uninstalling removes only the unchanged entries the CLI wrote. If you already have an entry with the same server id, the CLI reports a conflict and leaves it alone: rename or remove your entry, or set `mcpServers.<id>.enabled` to `false` in `.cratis/ai.json`. The Pi `cratis-mcp` extension supports only Screenplay, so other servers are reported as unsupported for Pi. Screenplay must launch the bundled `cratis screenplay mcp` command: a corpus descriptor that changes its executable or arguments is rejected before installation writes anything. A selected server that cannot be registered (a transport other than `stdio`, an incomplete descriptor, an id outside `[A-Za-z0-9_-]+`, a duplicate id, or a non-string `env` value) is skipped with a message in `unsupportedMcpServers` while the other servers install; malformed entries of unselected profiles are ignored. Any `1.x` descriptor `schemaVersion` is accepted and unknown fields are ignored; another major version registers no MCP servers and reports one message. Install/update creates the selected empty model directory (normally `.cratis/screenplay`), but does not start the server or create source files. Unsupported MCP adapters are named in `unsupportedMcpServers`; guidance installation does not imply every host has MCP support.

Commit all of it, including the installed `.cratis/ai/` tree, so everyone on the team and every CI run gets identical guidance.

The CLI owns only the `schemaVersion`, `harnesses`, `profiles`, `languages` and `mcpServers` sections of `.cratis/ai.json`. A corpus extension may own additional top-level sections, such as an update policy, and `install` and `update` keep them exactly as they are.

### Choosing a selection

Three dimensions decide what you receive. Only the first two normally matter.

Run `cratis ai profiles list` to see every profile the corpus offers.

| Dimension | What it selects | Required |
|---|---|---|
| `profiles` | Which body of guidance applies — the important decision | Yes |
| `harnesses` | Which coding agents get an adapter | Yes |
| `languages` | Narrows the languages composed by the selected profiles | No |

**Profiles** are the choice worth thinking about, and the split is *what you are building*:

- **`cratis/application/*`** — you are **building an application on Cratis**: vertical slices, commands and read models, Chronicle and Arc, a React frontend. Pick this for product repositories.
- **`cratis/engineering/*`** — you are **contributing to a Cratis framework repository itself**: libraries, source generators, the Chronicle kernel, client SDKs. The application architecture rules deliberately do not apply here.
- **`cratis/documentation`** — add alongside either when the repository contains documentation.

Getting this wrong is the most common mistake: an application repository configured with engineering profiles will not receive the slice guidance, and a framework repository configured with application profiles receives a manual that tells it not to apply.

**Harnesses** cost nothing to add. Selecting a tool you do not use only creates an unused adapter, so most repositories select all of them.

**Languages** is optional and narrowing, not additive. Omit it and no language constraint is applied, so every language a selected profile composes is installed. Name `csharp,typescript` and a Kotlin or Elixir rule composed by the same profile is skipped. Use it when a repository is genuinely single-language and you want less guidance loaded.

### Interactive and non-interactive

In a terminal, each dimension is a multi-select prompt:

```bash
cratis ai install
```

When input is redirected — CI, scripts, an agent — prompting is impossible, so `--profiles` and `--harnesses` must be passed. `--languages` may be omitted, which means unconstrained:

```bash
# An application repository, all harnesses
cratis ai install \
  --harnesses claude,codex,copilot,cursor,opencode,pi \
  --profiles cratis/application/csharp,cratis/documentation \
  --languages csharp,typescript

# A framework repository, pi only
cratis ai install --harnesses pi --profiles cratis/engineering/csharp

# Deterministic and offline: read the corpus from a local checkout
cratis ai install --harnesses pi --profiles cratis/documentation --source ../AI
```

### Options

| Option | Description |
|---|---|
| `--profiles <NAMES>` | Comma-separated profiles. Required when input is redirected. |
| `--harnesses <NAMES>` | Comma-separated harnesses: `claude`, `codex`, `copilot`, `cursor`, `opencode`, `pi`. Required when input is redirected. |
| `--languages <NAMES>` | Comma-separated languages. Optional; omitting it applies no language constraint. |
| `--source <PATH>` | Read the corpus from a local checkout of Cratis/AI instead of downloading the published one. Use it to pin a revision, work offline, or make a CI run deterministic. Falls back to `CRATIS_AI_SOURCE`, then to the published download. |
| `-f\|--force` | Replace or remove Cratis-managed files that were edited locally. Without it, a modified managed file is reported and nothing is written. |
| `--dry-run` | Report the changes that would be made and write nothing. The output is the same shape as a real run, so `--output json` works unchanged. |

Shell completion for the three selection options retains earlier comma-separated choices and suggests the next value without repeating one already chosen. It reads an installed local `.cratis/ai/manifest.json` or `profile-catalog.json`; without one it offers a small offline set of common choices. These are suggestions, not the authoritative set of choices in the source corpus. Pressing Tab never downloads the corpus.

### Templates come preconfigured

The [Cratis `dotnet new` templates](https://www.cratis.io/templates/) ship a `.cratis/ai.json` with the right profiles, languages and harnesses already chosen, and `dotnet new` prints a reminder. If you scaffolded from a template, skip `install` — the selection exists already — and run:

```bash
cratis ai update
```

## `cratis ai update`

Re-synchronizes using the selection already in `.cratis/ai.json`, so it takes no dimension options. Run it to pick up a newer corpus. To change the selection, run `install` again.

Update only touches files the manifest records as Cratis-managed, and never overwrites your edits: a modified managed file is reported as a conflict and nothing is changed until you pass `--force` or restore the file. Accepts `--source`, `--force` and `--dry-run`.

### Seeing what an update will do first

The corpus is not pinned on this channel: an update takes whatever the source currently holds. `--dry-run` reports exactly what would change and writes nothing, so a run can be reviewed before it happens.

```bash
cratis ai update --dry-run
```

The report is the same `actions` and `conflicts` the real run produces, plus `dryRun: true`, so it can be diffed or piped. The one conflict a dry run cannot predict is a script that must become executable when you are not allowed to change its file mode: checking that would touch the file.

```bash
cratis ai update --dry-run --output json | jq -r '.actions[]'
```

It is available on `install` and `uninstall` too. On `uninstall` it lists what would be removed without removing it.

### Knowing when an update is waiting

When you run a `cratis` command interactively in a directory with Cratis AI installed, the CLI may ask GitHub in the background how the commit recorded in `.cratis/ai.manifest.json` compares with the default branch of Cratis/AI. If an answer arrives in time, the command may be followed by:

```text
↑ Cratis AI update available: 12 new commits since cc9c631 - run 'cratis ai update'
```

The corpus has no version number, so the hint counts commits. It appears only when the installed commit is one the default branch has moved past. A commit from a local checkout that is not on GitHub, from another branch, or ahead of the default branch never produces it; `cratis ai status` still compares revisions directly.

The check reads `.cratis/ai.json` and `.cratis/ai.manifest.json`, changes nothing in the repository, and records its answer in the CLI's own update cache, `~/.cratis/version-check.json`, on the same schedule as the CLI's update check, keyed by the installed commit so another installation is always compared on its own. It is skipped when `CRATIS_AI_SOURCE` points at a local checkout, when the installed revision is not a Git commit, for `cratis ai install`, `update` and `uninstall`, and wherever the CLI's own update hint is skipped. `CRATIS_NO_UPDATE_CHECK` switches it off; see [Global Options](../reference/global-options.md#update-hints-and-cratis_no_update_check).

## `cratis ai status`

Read-only. Shows the configured profiles, harnesses and languages, the revision installed here, the revision available from the source, whether an update is available, and any managed file modified locally. Exits non-zero when local modifications exist, which makes it usable as a CI check that guidance has not drifted.

```bash
cratis ai status --output json
```

## `cratis ai profiles list`

Read-only: lists public and engineering profiles with their catalog, languages and composed-profile count. Use `--output json` for each profile's full `id` and `catalog` values and `languages` and `composes` arrays. `--source` and `CRATIS_AI_SOURCE` select the corpus just as they do for `install`.

```bash
cratis ai profiles list --output json
```

## `cratis ai uninstall`

Removes Cratis-managed files and the harness adapters Cratis created, preserving user-owned files. Modified managed corpus files are reported as conflicts; `--force` removes those files anyway. MCP entries are stricter: changed owned entries and foreign entries are never overwritten or removed by `--force`. Model directories and their contents are retained. Use `--dry-run` to list what would be removed first.

## How update and uninstall stay precise

Every installed file carries a managed marker, and `.cratis/ai.manifest.json` records each file's source and content hash. That is what lets `update` and `uninstall` distinguish their own files from yours, act only on Cratis-installed content, and detect a local edit rather than silently overwriting it.

A managed file's execute bit follows the corpus: a file the corpus ships as executable, or that starts with a shebang, is made executable for everyone who can read it, and any other managed file has its execute bits removed. Run `cratis ai update` to repair scripts an older CLI installed without their execute bit.

Executable scripts keep their shebang on the first line, with the managed comment immediately after it. If an older CLI installed a JavaScript hook with the marker above its shebang, update the CLI and run `cratis ai update` to regenerate it. Unchanged managed files need no `--force`; do not repair the installed hook by hand.

## Troubleshooting

| Condition | Result |
|---|---|
| `No .cratis/ai.json exists` from `update` or `status` | The repository has no selection yet — run `cratis ai install`. |
| Unknown harness, profile or language passed to `install` | Validation error listing what the corpus offers. |
| `--profiles` or `--harnesses` missing from `install` with input redirected | Validation error: the option is required when prompting is impossible. |
| Modified managed files, or a user-owned path in the way, without `--force` | Conflict report, and no files are changed. |
| An installed script must become executable, but its mode cannot be changed (another user owns it, or the file system refuses every mode change) | Conflict report, and no files are changed. `--force` does not help: the file's owner runs the update, or the project moves to a file system that keeps file modes. |
| The file system ignores mode changes, or refuses to make a script executable while allowing its current mode to be set again | The update completes and lists each script it could not make executable under `warnings`. Run those scripts through their interpreter (`bash`, `node`). |
| `status` reports `updateAvailable` but `update` changes nothing | The corpus revision moved without changing any file this selection receives. |
