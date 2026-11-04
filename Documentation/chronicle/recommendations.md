---
description: List the health and performance recommendations Chronicle generates for an event store, then perform or ignore them with cratis chronicle recommendations.
---

# Recommendations

Chronicle returns recommendations with IDs, descriptions, and suggested actions. Use `recommendations list` to read what the server suggests for the selected event store and namespace, then investigate the affected observer, failure, or job before deciding what to do.

A recommendation can help you find a problem; it does not establish the cause or authorize recovery. For a view that stopped updating, the [stuck-observer guide](../scenarios/fix-a-stuck-observer.md) connects the inspection to a supported next action.

## list

Lists active recommendations for the selected event store and namespace. Confirm the authorized server and context before using these examples. Read the description before taking an ID into `perform` or `ignore`; the ID alone does not tell you what the action will change.

```bash
cratis chronicle recommendations list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Examples

List all recommendations:

```bash
cratis chronicle recommendations list
```

List in plain format:

```bash
cratis chronicle recommendations list --output plain
```

Get recommendation IDs for scripting:

```bash
cratis chronicle recommendations list -q
```

## perform

Asks the server to perform the action for one recommendation ID in the selected event store and namespace. Agree on that specific action and its effects with the responsible owner first. If it reruns handlers, approval must cover any repeated external effects too.

```bash
cratis chronicle recommendations perform <RECOMMENDATION_ID>
```

The command prompts before proceeding. `--yes` skips that confirmation for an already authorized automated action; it does not grant permission. A successful response reports the recommendation performed, not that the affected view is current. Inspect the relevant observer, failed partition, and model/key afterward. If you have identified a background job for the operation, [jobs get](jobs.md#get) provides its status and error details.

### Arguments

| Argument | Description |
|---|---|
| `RECOMMENDATION_ID` | The GUID of the recommendation to perform. Use `recommendations list -q` to retrieve IDs. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Perform the one recommendation you have inspected and agreed to act on:

```bash
cratis chronicle recommendations perform a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

For an authorized automated action, skip confirmation for that same recommendation ID:

```bash
cratis chronicle recommendations perform a1b2c3d4-e5f6-7890-abcd-ef1234567890 --yes
```

## ignore

Marks one recommendation as ignored, removing it from the active list without performing its suggested action. This changes the recommendation list; it does not repair the condition that prompted the suggestion.

```bash
cratis chronicle recommendations ignore <RECOMMENDATION_ID>
```

Agree with the owner before dismissing a recommendation others may be investigating. The command prompts for confirmation; skipping the prompt does not supply that agreement.

### Arguments

| Argument | Description |
|---|---|
| `RECOMMENDATION_ID` | The GUID of the recommendation to ignore. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Ignore a recommendation:

```bash
cratis chronicle recommendations ignore a1b2c3d4-e5f6-7890-abcd-ef1234567890
```
