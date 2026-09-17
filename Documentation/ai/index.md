# Cratis AI

`cratis ai` installs and maintains Cratis-owned AI guidance — rules, skills, agents, prompts, hooks, and harness integration — in a repository. The content comes from the [Cratis AI](https://github.com/Cratis/AI) corpus.

The idea is one shared corpus and many thin adapters. Guidance is installed once, into `.cratis/ai/`, and every coding agent you use is pointed at that same copy. Claude Code, Codex, Copilot, Cursor, OpenCode and pi each read guidance from their own conventional location, so each gets a native adapter that links back to the one corpus. Nothing is duplicated per tool, and updating the corpus updates every tool at once.

## Commands

```bash
cratis ai install      # choose what this repository needs, and install it
cratis ai update       # re-synchronize the choice already recorded
cratis ai status       # what is configured, installed, available, and locally modified
cratis ai uninstall    # remove unchanged Cratis-managed content
```

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

Adapters are symbolic links into `.cratis/ai/`, not copies. A user-owned file already sitting at one of those paths is left exactly as it is — install never replaces your own content.

Commit all of it, including the installed `.cratis/ai/` tree, so everyone on the team and every CI run gets identical guidance.

### Choosing a selection

Three dimensions decide what you receive. Only the first two normally matter.

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

### Templates come preconfigured

The [Cratis `dotnet new` templates](https://www.cratis.io/templates/) ship a `.cratis/ai.json` with the right profiles, languages and harnesses already chosen, and `dotnet new` prints a reminder. If you scaffolded from a template, skip `install` — the selection exists already — and run:

```bash
cratis ai update
```

## `cratis ai update`

Re-synchronizes using the selection already in `.cratis/ai.json`, so it takes no dimension options. Run it to pick up a newer corpus. To change the selection, run `install` again.

Update only touches files the manifest records as Cratis-managed, and never overwrites your edits: a modified managed file is reported as a conflict and nothing is changed until you pass `--force` or restore the file. Accepts `--source` and `--force`.

## `cratis ai status`

Read-only. Shows the configured profiles, harnesses and languages, the revision installed here, the revision available from the source, whether an update is available, and any managed file modified locally. Exits non-zero when local modifications exist, which makes it usable as a CI check that guidance has not drifted.

```bash
cratis ai status --output json
```

## `cratis ai uninstall`

Removes Cratis-managed files and the harness adapters Cratis created, preserving user-owned files. Modified managed files are reported as conflicts; `--force` removes them anyway.

## How update and uninstall stay precise

Every installed file carries a managed marker, and `.cratis/ai.manifest.json` records each file's source and content hash. That is what lets `update` and `uninstall` distinguish their own files from yours, act only on Cratis-installed content, and detect a local edit rather than silently overwriting it.

## Troubleshooting

| Condition | Result |
|---|---|
| `No .cratis/ai.json exists` from `update` or `status` | The repository has no selection yet — run `cratis ai install`. |
| Unknown harness, profile or language passed to `install` | Validation error listing what the corpus offers. |
| `--profiles` or `--harnesses` missing from `install` with input redirected | Validation error: the option is required when prompting is impossible. |
| Modified managed files, or a user-owned path in the way, without `--force` | Conflict report, and no files are changed. |
| `status` reports `updateAvailable` but `update` changes nothing | The corpus revision moved without changing any file this selection receives. |
