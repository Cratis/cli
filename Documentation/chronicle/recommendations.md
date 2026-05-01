# Recommendations

Recommendations are server-generated suggestions for actions that may improve the health or performance of an event store. Chronicle analyzes its own state — observer positions, failed partitions, stuck jobs, and sequence anomalies — and produces recommendations when it detects conditions that require attention. Examples include replaying a stuck observer, retrying a repeatedly failing partition, or rebuilding an index.

Each recommendation has a GUID identifier, a description, and a suggested action. You can either perform the action (let Chronicle execute it server-side) or ignore the recommendation.

## list

Lists all active recommendations for the specified event store and namespace.

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

Instructs the server to perform a recommended action. Chronicle executes the action (for example, triggering an observer replay) and removes the recommendation on success.

```bash
cratis chronicle recommendations perform <RECOMMENDATION_ID>
```

The command prompts for confirmation before proceeding. Pass `--yes` to skip the prompt in automated workflows.

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

Perform a recommendation interactively:

```bash
cratis chronicle recommendations perform a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Perform without confirmation (automation):

```bash
cratis chronicle recommendations perform a1b2c3d4-e5f6-7890-abcd-ef1234567890 --yes
```

Perform all recommendations in sequence:

```bash
cratis chronicle recommendations list -q | xargs -I {} cratis chronicle recommendations perform {} --yes
```

## ignore

Marks a recommendation as ignored. Chronicle removes it from the active list without taking any action.

```bash
cratis chronicle recommendations ignore <RECOMMENDATION_ID>
```

The command prompts for confirmation before proceeding.

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
