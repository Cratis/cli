---
description: Run opt-in, deterministic AI conformance fixtures in temporary workspaces without changing the invoking repository.
---

# Test AI conformance

`cratis ai test` runs deterministic fixture checks against isolated harness sessions. It is opt-in: invoking this command can spend model quota. Ordinary installation, builds and specs do not run it.

```bash
cratis ai test --source ../AI --harnesses claude,codex --output json
cratis ai test --fixture example --timeout 60 --max-budget-usd 0.25
```

The command reads the current repository's `.cratis/ai.json` and installation manifest. Missing installations and modified managed files are rejected. `--source` (or `CRATIS_AI_SOURCE`) selects a corpus checkout and permits corpus-CI mode without a clean installation. Without a repository configuration, corpus-CI mode selects the corpus's available profiles and languages. The default source otherwise follows `cratis ai install`.

For every matching fixture and harness, fixture input is copied into a fresh temporary workspace and the selected profiles, languages and harness adapter are installed through the normal installer. The invoking repository is not changed. This is filesystem separation, **not a security boundary**: fixtures, their command checks and harnesses must be trusted. Harness authentication and host tools remain available. User-memory isolation is reported as `unknown`.

## Options

| Option | Meaning |
| --- | --- |
| `--harnesses claude,codex` | Select adapters; defaults to supported adapters in the configuration. |
| `--fixture <name>` | Select a fixture by name; repeat to select several. |
| `--fixtures <directory>` | Override the default `Evaluations/conformance` root in the corpus. |
| `--source <directory>` | Use a local corpus checkout. |
| `--output text\|json` | Human-readable summary or structured report. |
| `--timeout <seconds>` | Lower, never raise, a fixture's timeout. |
| `--max-budget-usd <amount>` | Lower, never raise, a fixture's requested model budget. |
| `--keep-transcripts` | Retain redacted transcripts under the run's temporary output directory. |

Claude uses project settings, stream JSON, no session persistence and `acceptEdits`; it does not use `--bare`. Budget and turn flags are supplied when requested. Codex uses `exec --json --ephemeral --ignore-user-config --sandbox workspace-write`; its budget and turn limits are reported as `not_enforced`, not guaranteed. Timeout enforcement applies to both. Missing executables, credentials and unavailable modes are never passes.

## Fixture format

Each `<fixtures-root>/<name>/fixture.json` can have a sibling `input/` tree. For example:

```json
{
  "name": "example",
  "description": "Produce a file using installed guidance",
  "prompt": "Create result.txt containing ready.",
  "profiles": [],
  "languages": [],
  "harnesses": ["claude", "codex"],
  "expected_instruction_paths": [".cratis/ai/rules/general.md"],
  "checks": [
    {"type": "file_exists", "path": ".cratis/ai/rules/general.md", "phase": "install"},
    {"type": "file_matches", "glob": "**/result.txt", "pattern": "ready", "phase": "result"}
  ],
  "bounds": {"timeout_seconds": 120, "max_budget_usd": 0.5, "max_turns": 5}
}
```

Empty profile/language/harness restrictions mean any selection. `expected_output`, if supplied, is ignored: there is no model judge. Every fixture must have at least one non-optional result-phase check. Invalid fixtures produce errors, not passes.

Checks support `file_exists`, `file_absent`, `file_matches`, `file_not_matches` and `command`. File checks require exactly one `path` or `glob`. Globs are relative to the workspace root: `*` stays within a directory and `**` crosses directories. Absolute paths, `..` and escaping links are rejected. Result-phase checks exclude `.git/`, `.cratis/` and `node_modules/`; install-phase checks can explicitly target `.cratis/`. Directory links are not traversed by globs.

`phase` is `install` or `result` (default). Install-phase checks run before the harness, and cannot be commands. Patterns use .NET regex per file, multiline mode off by default, with a one-second match timeout. Every selected file must satisfy the regex check. No matching files fails a regex check; `file_matches` reports `zero files matched` distinctly.

Command checks use `run: ["executable", "argument", "..."]`, without an implicit shell, and an expected `exit_code` (default zero). An `optional` command check is skipped if its executable is missing; other errors and mismatched exit codes still fail. Processes have timeout/cancellation handling that kills their process trees.

## Results and retention

Each report separates installed instruction paths/hashes from `observed_load`. Only an explicit tool-read event naming a path establishes `observed`; other paths are `unknown`. Passing checks and installed hashes do not prove that instructions were loaded. Each bound includes its value and enforcement status: `enforced`, `not_enforced` or `not_requested`.

Exit codes are 0 for all passed/skipped or no selected runs, 1 for check, harness or fixture failures/errors, and 2 for usage or installation errors. When there is no passed result, the report warns `no fixtures ran successfully`; skipped results never count as passes.

Transcripts are not retained by default. With `--keep-transcripts`, the report identifies a file under a temporary output directory; the workspace is still removed. Environment values whose variable names contain KEY, TOKEN or SECRET, and recognizable token formats, are redacted. Redaction is best-effort, not a promise that arbitrary sensitive text can be detected; inspect retained files before sharing them.
