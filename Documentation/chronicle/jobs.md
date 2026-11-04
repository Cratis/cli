---
description: Inspect, resume, stop and delete the long-running server-side jobs Chronicle runs, such as observer replays, with cratis chronicle jobs.
---

# Jobs

A command that starts background work does not tell you when that work has finished. Use `jobs list` to inspect available jobs, then `jobs get` with a returned ID to read its status, steps, progress, and errors. These commands do not establish which job belongs to a replay. Without an already-established job ID for the replay, verify it through `observers show` and the expected model/key instead; do not choose a job by type or creation time alone.

Inspect first, then agree with the responsible owner before stopping, resuming, or deleting a job. Those commands serve different purposes: stop preserves progress, resume continues an eligible stopped job, and delete requests irreversible removal of the retained job and its state. Deleting a job is not a way to repair a stuck view.

## list

Lists jobs in the selected event store and namespace, including running and completed jobs. Confirm the authorized server and context before using the examples, then take the returned job ID into `jobs get`. An empty successful list differs from a query that failed; resolve the latter before concluding that no job exists.

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

Shows one job's status, steps, progress, and errors. Use the ID returned by `jobs list` for the same event store and namespace. If a step failed, read its error before deciding whether to change the job. Even a completed job does not by itself prove that the expected view is correct; check the relevant model/key or client-owned application too.

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

Requests that a stopped job continue from where it left off. A failed job cannot be resumed; inspect its errors instead. The server decides whether the requested transition is allowed.

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

The command asks for confirmation and defaults to **No**. For an authorized non-interactive action, pass `--yes` explicitly; otherwise it returns a nonzero validation exit code without resuming the job.

The server reports whether it accepted the resume. If it cannot find the job, resume it, or save the changed state, the CLI reports the reason in text and JSON and exits nonzero. An accepted request is not finished processing: use `jobs get` to check progress and errors, then verify the expected view. Requires Chronicle 19.80.0 or later.

## stop

Requests that a running job stop while preserving its progress. A stopped job can later be resumed if the server allows it. Check the job's current state and agree on the interruption before running the command.

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

The command asks for confirmation and defaults to **No**. For an authorized non-interactive action, pass `--yes` explicitly; otherwise it returns a nonzero validation exit code without stopping the job.

The server reports whether it accepted the stop. If the job is missing, already finished, not running, or the server cannot save the change, the CLI reports the reason in text and JSON and exits nonzero. It does not report a refused stop as successful. Requires Chronicle 19.80.0 or later.

After an accepted stop, use `jobs get` to confirm the current status rather than assuming the view is fixed:

```bash
cratis chronicle jobs get a1b2c3d4-e5f6-7890-abcd-ef1234567890 -o json
```

## delete

Requests irreversible deletion of the retained job and its state. Use this only when removal itself is the intended, authorized action. It cannot be undone and is not a substitute for stopping, resuming, or investigating a failure.

```bash
cratis chronicle jobs delete <JOB_ID>
```

### Arguments

| Argument | Description |
|---|---|
| `JOB_ID` | The GUID of the job to delete. Use `jobs list -q` to retrieve job IDs. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to `Default`. |
| `-y, --yes` | Skip local confirmation; does not bypass server authorization or policy. |

### Examples

Delete a job in a specific scope, with explicit confirmation for automation and JSON output:

```bash
cratis chronicle jobs delete a1b2c3d4-e5f6-7890-abcd-ef1234567890 -e my-store -n my-namespace --yes -o json
```

The confirmation names the resolved event store, namespace and job ID, explains that the retained job and its state are removed, and defaults to **No**. Declining, or running non-interactively without `--yes`, returns a nonzero exit code before connecting. An invalid or missing job ID also fails locally without calling the server.

The server enforces authorization and policy and owns the audit. The CLI returns a nonzero exit code for authorization failures, validation refusals, and errors. A successful command says only **Deletion requested**: the contract acknowledges the request, not a deletion outcome. It does not prove that a job existed or that its state was removed. JSON success output includes `eventStore`, `namespace`, `jobId`, and `message`.
