# Cratis AI

`cratis ai` installs and maintains Cratis-owned AI guidance — rules, skills, agents, prompts, hooks, and harness integration — in a repository. The content comes from the [Cratis AI](https://github.com/Cratis/AI) corpus, and what gets installed is selected by three dimensions recorded in the project-owned `.cratis/ai.json`:

| Dimension | What it selects |
|---|---|
| `harnesses` | Which coding agents to integrate: `claude`, `codex`, `copilot`, `cursor`, `opencode`, `pi` |
| `profiles` | Which Cratis AI profiles to resolve — for example `cratis/application/chronicle-dotnet` or `cratis/application/csharp` |
| `languages` | Which language rules apply — for example `csharp` or `typescript` |

```json
{
  "schemaVersion": "1.0",
  "harnesses": [
    "claude",
    "codex",
    "copilot",
    "cursor",
    "opencode",
    "pi"
  ],
  "profiles": [
    "cratis/application/csharp"
  ],
  "languages": [
    "csharp",
    "typescript"
  ]
}
```

## Templates come preconfigured

The [Cratis `dotnet new` templates](https://www.cratis.io/templates/) ship a `.cratis/ai.json` with the correct profiles, languages, and all harnesses for the scaffolded project, and `dotnet new` prints a reminder after creation. If you scaffolded from a Cratis template, all that is needed is the CLI itself — see [cratis.io/cli](https://cratis.io/cli/) — and:

```bash
cratis ai update
```

This installs the AI rules, skills, and harness integration and records what it installed in `.cratis/ai.manifest.json`. Commit `.cratis/ai.json`, `.cratis/ai.manifest.json`, and the installed `.cratis/ai/` content so the whole team shares the same guidance.

## Commands

```bash
cratis ai install      # select dimensions and install
cratis ai update       # synchronize the configured selection
cratis ai status       # configuration, installed revision, local conflicts
cratis ai uninstall    # remove unchanged Cratis-managed content
```

### `cratis ai install`

Selects the dimensions and installs the corpus into the current repository. In an interactive terminal, each dimension is a multi-select prompt. When input is redirected — CI, scripts — all three must be passed as options.

| Option | Description |
|---|---|
| `--harnesses <NAMES>` | Comma-separated harness list, e.g. `claude,codex`. Required when input is redirected. |
| `--profiles <NAMES>` | Comma-separated profile list. Required when input is redirected. |
| `--languages <NAMES>` | Comma-separated language list. Required when input is redirected. |
| `--source <PATH>` | Path to a local checkout of Cratis/AI. Makes installs deterministic in CI and offline environments. Defaults to the `CRATIS_AI_SOURCE` environment variable, then to the published download. |
| `-f\|--force` | Replace or remove Cratis-managed files that were modified locally. |

### `cratis ai update`

Re-synchronizes the corpus using the selection already recorded in `.cratis/ai.json` — no dimension options. Use it after scaffolding from a Cratis template, and whenever you want the latest guidance. Accepts the same `--source` and `--force` options as `install`.

Update only touches files recorded as Cratis-managed in the manifest, and never overwrites local changes: if a managed file was edited, the command reports it as a conflict and changes nothing until you pass `--force` or restore the file.

### `cratis ai status`

Shows the configured harnesses, profiles, and languages, the revision installed locally, the revision available from the source, whether an update is available, and any managed files modified locally. Exits non-zero when local modifications exist.

### `cratis ai uninstall`

Removes Cratis-managed files and harness integration that are still unchanged, while preserving user-owned files. Modified managed files are reported as conflicts; pass `-f\|--force` to remove them anyway.

## What gets installed

Managed content lives under `.cratis/ai/`:

- `rules/`, `skills/`, `agents/`, `prompts/`, `hooks/` — resolved from the selected profiles and harnesses
- `harnesses/<harness>/` — harness-specific content, such as Cursor rules or pi extensions

Harness integration is created as symbolic links into that content — for example `AGENTS.md` and `CLAUDE.md` pointing at the project rules, and `.claude/`, `.cursor/`, `.github/`, `.opencode/`, or `.pi/` folders linking rules, skills, agents, prompts, and hooks. Existing user-owned files at those paths are preserved, never replaced.

Every installed file carries a managed marker, and `.cratis/ai.manifest.json` records each file's source and hash. That manifest is what makes `update` and `uninstall` precise: they only ever act on content Cratis installed.

## Errors

| Condition | Result |
|---|---|
| No `.cratis/ai.json` when running `update` or `status` | Validation error — run `cratis ai install` first. |
| Unknown harness, profile, or language passed to `install` | Validation error listing what the corpus offers. |
| Dimension options missing from `install` in a non-interactive terminal | Validation error — the option is required when input is redirected. |
| Modified managed files or conflicting user-owned paths, without `--force` | Conflict report; no files are changed. |
