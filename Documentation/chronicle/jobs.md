# Jobs

Jobs are long-running server-side operations that Chronicle executes asynchronously. Common examples include observer replays, index rebuilds, and partition recovery operations. Chronicle tracks each job's progress, individual steps, and final status. Jobs may be stopped, resumed, and inspected while they are running or after they complete.

## list

Lists all jobs in the specified event store and namespace, including both running and completed jobs.

```bash
cratis chronicle jobs list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Examples

List all jobs:

```bash
cratis chronicle jobs list
```

List jobs in plain format:

```bash
cratis chronicle jobs list --output plain
```

Get job IDs for scripting:

```bash
cratis chronicle jobs list -q
```

## get

Shows full details for a single job, including all steps, their individual progress, and any error information.

```bash
cratis chronicle jobs get <JOB_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `JOB_ID` | The GUID of the job. Use `jobs list -q` to retrieve job IDs. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Examples

Show job details:

```bash
cratis chronicle jobs get a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Get the full job document as JSON:

```bash
cratis chronicle jobs get a1b2c3d4-e5f6-7890-abcd-ef1234567890 -o json
```

## resume

Resumes a stopped or failed job, allowing it to continue from where it left off.

```bash
cratis chronicle jobs resume <JOB_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `JOB_ID` | The GUID of the job to resume. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |

### Examples

Resume a stopped job:

```bash
cratis chronicle jobs resume a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

## stop

Stops a running job. The job's progress is preserved and the job can be resumed later.

```bash
cratis chronicle jobs stop <JOB_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `JOB_ID` | The GUID of the job to stop. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |

### Examples

Stop a running job:

```bash
cratis chronicle jobs stop a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Check the job status after stopping:

```bash
cratis chronicle jobs get a1b2c3d4-e5f6-7890-abcd-ef1234567890 -o json
```
