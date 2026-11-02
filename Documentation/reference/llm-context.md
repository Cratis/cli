---
title: LLM context
description: The machine-readable command catalog printed by cratis llm-context, including whether each command changes state.
---

`cratis llm-context` prints the CLI's command catalog as JSON for AI agents and tools: every command group, command, argument, and option, together with connection details, usage tips, and output-format guidance. `cratis init` embeds the same catalog in the `chronicle-cli` skill it installs.

## Command

```bash
cratis llm-context            # the live catalog
cratis llm-context --schema   # the JSON Schema that describes the catalog
```

The command always prints JSON, regardless of `--output`.

## Command effects

Every command in the catalog carries an `effect` field that states the strongest change the command can make. Classify commands by this field instead of keeping a hand-maintained list of read-only commands; a new command cannot ship without declaring it.

| `effect` | Meaning | Examples |
|---|---|---|
| `read-only` | Observes only. Reads from the Chronicle server, an application, or local files and changes nothing. | `chronicle observers list`, `context show`, `screenplay validate`, `screenplay baseline check` |
| `local` | Changes only the local machine: CLI configuration and contexts, cached credentials, files in the working directory, shell configuration, installed tools, or local containers. Never changes server or store state. | `context set`, `chronicle login`, `init`, `ai install`, `screenplay baseline admit` |
| `mutating` | Changes Chronicle server or store state without removing or resetting existing state. | `chronicle users add`, `chronicle jobs stop`, `chronicle observers retry-partition`, `chronicle observers clear-partition-quarantine` |
| `destructive` | Removes or resets Chronicle server or store state. | `chronicle users remove`, `chronicle observers replay`, `chronicle recommendations perform` |

The value is the strongest effect the command can have. An option such as `--dry-run` can lower the effect of a single run, but never raise it. Caches the CLI keeps for itself, such as authentication tokens and update checks, do not count as an effect.

Treat every value other than `read-only` as a state change. `screenplay conform` checks source against an authored model without writing a recovered `.play` file, but declares `local` because its MSBuild source-loading pipeline may create intermediate build files. It uses check-specific exit codes (0 clean, 1 defects, 2 could not run/incomplete); see [Screenplay conformance](screenplay.md#cratis-screenplay-conform-model_root). `screenplay conform-namespace` is `read-only`: it reads Chronicle and exits 0 when the comparison ran, or 7 (drift) and 8 (inconclusive) with `--fail-on-drift`; see [Screenplay namespace conformance](screenplay.md#cratis-screenplay-conform-namespace-model_root).

## Confirmation

Every command also carries a boolean `requiresConfirmation` field. When it is `true`, the command asks for confirmation in an interactive terminal, and in a non-interactive environment it fails with a validation error unless you pass `--yes` (see [Global Options](global-options.md)).

```json
{
  "name": "remove",
  "description": "Removes a user from the Chronicle server. Destructive — prompts for confirmation unless --yes is specified.",
  "effect": "destructive",
  "requiresConfirmation": true,
  "arguments": [
    { "name": "<USER_ID>", "type": "guid", "description": "The unique identifier of the user to remove (positional)" }
  ]
}
```

`requiresConfirmation` is independent of `effect`: some `local` commands, such as `context delete` and `llm clear`, also ask first, and the interactive `chronicle workbench` confirms its actions in its own dialogs rather than through `--yes`.

## Permission and bounded inspection

Use this contract alongside the [first-inspection tutorial](../getting-started/index.mdx) or [missing-projection workflow](../scenarios/find-a-missing-projection.md). A catalog describes capabilities, not a user's target, authority, or inspection budget.

`effect` and `requiresConfirmation` are **not permission**. Obtain approval for the exact target and action before any state change, even if it has no prompt. Treat `read-only` as a command classification, not a promise of zero local initialization or zero sensitive output. Treat unknown effects as state-changing.

Before issuing requests, establish:

1. The authorized server, event store, namespace, relevant identifiers, and data-sharing boundary. Resolve `--server` → `CHRONICLE_CONNECTION_STRING` → context → localhost; `context show` is stored state, not effective-target proof. See [connection resolution](connection.md).
2. A small request/page/range/time budget and stopping condition. Prefer one `diagnose` sweep, one exact identifier/key, one small model page, or one approved recent event start/output interval. For event reads, choose `--from` near the approved observed tail; `--to` limits output only and does not cap retrieval. An approved finite request count does not cap an individual request's transfer or server work. Do not use unbounded log reads, automatic paging, all-store expansion, or watch loops as fallback discovery.
3. The required evidence. A successful empty result differs from an unavailable query. `get-started` exit code `0` alone does not prove connectivity; check `canConnect` and the displayed-server limitation in [Getting started](../getting-started/index.mdx). Diagnostic completeness is separate from health.

For a missing projection, the ownership chain is **store declaration → read-model identifier/owner and linked observer → namespace runtime state → bounded instance/key lookup**. Projection/read-model definition lists ignore namespace. If the declaration is absent, stop and check the owning application's registration. If model state is client-owned, stop Chronicle instance retrieval and hand off to that application. Don't invent definitions or derive query identifiers from display names.

Report unknown state when checks cannot run. Preserve the target and minimal sanitized evidence; don't paste credentials, context files, or unnecessary event/model payloads into a prompt. Stop when the budget is reached and ask before widening scope. No finding, recommendation, `--yes` flag, or catalog entry authorizes retry, quarantine clearing, replay, or recommendation execution.

Arc discovery has separate [application URL prerequisites](../arc/index.md). It reads endpoint metadata, not Chronicle state, and does not execute discovered routes.

## Stability

The field names `effect` and `requiresConfirmation` and the four `effect` values are stable. Parse `effect` as a string and treat an unknown value as a state change, so a consumer stays safe if a value is added later. The rest of the catalog describes the CLI version that printed it. Refresh any stored copy, such as the one `cratis init` writes, after upgrading the CLI.
