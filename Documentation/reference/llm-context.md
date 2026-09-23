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
| `read-only` | Observes only. Reads from the Chronicle server, an application, or local files and changes nothing. | `chronicle observers list`, `context show`, `screenplay validate` |
| `local` | Changes only the local machine: CLI configuration and contexts, cached credentials, files in the working directory, shell configuration, installed tools, or local containers. Never changes server or store state. | `context set`, `chronicle login`, `init`, `ai install` |
| `mutating` | Changes Chronicle server or store state without removing or resetting existing state. | `chronicle users add`, `chronicle jobs stop`, `chronicle observers retry-partition` |
| `destructive` | Removes or resets Chronicle server or store state. | `chronicle users remove`, `chronicle observers replay`, `chronicle recommendations perform` |

The value is the strongest effect the command can have. An option such as `--dry-run` can lower the effect of a single run, but never raise it. Caches the CLI keeps for itself, such as authentication tokens and update checks, do not count as an effect.

Treat every value other than `read-only` as a state change.

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

## Stability

The field names `effect` and `requiresConfirmation` and the four `effect` values are stable. Parse `effect` as a string and treat an unknown value as a state change, so a consumer stays safe if a value is added later. The rest of the catalog describes the CLI version that printed it. Refresh any stored copy, such as the one `cratis init` writes, after upgrading the CLI.
