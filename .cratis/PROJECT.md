# Cratis CLI — project context

The `cratis` command-line interface: scaffolds and operates Cratis
applications (`cratis init`, workbench, runtime management). C#/.NET,
published as a .NET tool. `CHRONICLE.md` at the repository root is generated
by `cratis init` and describes the current project's Chronicle server —
regenerate with `cratis init --refresh`.

## Commands

```bash
dotnet build
dotnet test
```

## Conventions

- Every `.cs` file starts with the two-line Cratis copyright header;
  file-scoped namespaces; `var`; records for data; no technical postfixes.
- American English everywhere.
- Prefer the `rtk` prefix for token-heavy shell commands when it is available
  (see https://github.com/cratis/rtk): `rtk <command>` passes through
  unchanged when no dedicated filter exists, so it is always safe.

## Command catalogue

The CLI's full command reference is embedded as the `chronicle-cli` skill by
`cratis init`; in repositories where it is installed (for example Studio and
Stagehand) it loads on demand under `.pi/skills/chronicle-cli/`.

## AI-assisted development

This repository uses the Cratis AI contract:

- **`.cratis/ai.json`** records the subscription — `cratis/documentation` plus the `cratis/engineering/csharp` maintainer cell.
- **`.cratis/PROJECT.md`** (this file) is the canonical project context; the root `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` are minimal bootstraps that point here and do nothing else.
- There is **no local AI corpus and no generated tool adapters** in this repository. Shared skills arrive through the Cratis AI marketplace plugins (Claude Code, Codex, GitHub Copilot, Cursor, and Pi are installable today — see the [harness guide](https://www.cratis.io/ai/harnesses/)).

For contributors:

1. Install the Cratis plugin for your harness once (per the harness guide); the subscribed profiles' skills then load automatically when tasks match.
2. General, reusable improvements are proposed in [`Cratis/AI`](https://github.com/Cratis/AI) — never copied into, or synchronized from, this repository.
3. Repository-specific facts and conventions belong in this file; repository-local skills live under `.agents/skills/`.
4. AI session work records (plans, handovers, session notes, scratch analyses) stay in the untracked `.ai-work/` folder and never enter git; a durable follow-up becomes a GitHub issue.
