---
title: Rebuild Cratis Chronicle constraint indexes with the CLI
description: Rebuild unique and closing constraint indexes from an event sequence with cratis chronicle constraints reindex, and optionally wait for the job to finish.
---

<a id="constraints"></a>

Chronicle enforces unique and closing constraints with indexes that it derives from the events in an event sequence. When those indexes are missing or out of step with the events, `cratis chronicle constraints reindex` asks Chronicle to rebuild them from the sequence. The rebuild runs as a server-side [job](jobs.md). It clears and recreates derived index state; it never deletes events.

## reindex

```bash
cratis chronicle constraints reindex [CONSTRAINT_NAME...] [-e|--event-store <NAME>] [-n|--namespace <NAME>] [--event-sequence <ID>] [--wait] [--yes] [-o|--output <FORMAT>]
```

The command needs a Chronicle administrator. If the server refuses it, run `cratis chronicle login <administrator-username>` and try again.

### Arguments

| Argument | Description |
|---|---|
| `CONSTRAINT_NAME` | Zero or more constraint names to rebuild. Names are sent as typed, in the order given. Omit them to rebuild all applicable unique and closing constraints for the event sequence. Chronicle chooses the applicable constraints; the CLI does not list them. |

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace. Defaults to the active context's namespace, then `Default`. |
| `--event-sequence <ID>` | Event sequence to rebuild from. Defaults to `event-log`. |
| `--wait` | Wait for the job to finish. Exits zero only when the job completed successfully. |
| `--yes` | Skip the confirmation prompt. |
| `-o, --output <FORMAT>` | Output format, for example `json`. |

### Examples

Rebuild every applicable constraint index for the default event sequence:

```bash
cratis chronicle constraints reindex
```

Rebuild two named constraints in a specific event store, namespace and event sequence, wait for the job, and print the result as JSON:

```bash
cratis chronicle constraints reindex --event-store orders --namespace West --event-sequence event-log UniqueOrderNumber ClosedOrder --wait --yes --output json
```

### While the job runs

Governed appends to the event sequence are refused while the job runs. Clients should retry those appends after the job ends. The command asks for confirmation and defaults to **No**. In automation or another non-interactive environment, pass `--yes` explicitly or the command returns a nonzero validation exit code without starting the job.

### Output

Without `--wait`, the command starts the job and prints the event sequence, the selected constraint names (or that all applicable constraints are selected) and the job ID, then exits zero. With `--output json` it prints one document with `eventSequenceId`, `constraintNames`, `allConstraints`, `jobId`, `jobName` and `status`. `constraintNames` is empty when all applicable constraints were selected.

If Chronicle rejects the request, for example because of an unknown constraint name, the command prints the server's message and exits nonzero without starting a job.

### Waiting for the job

With `--wait` the command polls the job by its ID until it reaches a final state. It never starts the reindex a second time while waiting. With JSON output it prints a single document when the job ends, with the final `status`.

| Job state | Result |
|---|---|
| Completed successfully | Exit code `0`. |
| Failed or completed with failures | Nonzero exit code. Inspect the job with `cratis chronicle jobs get <JOB_ID>`. |
| Stopped or being removed | Nonzero exit code. The job did not complete successfully. |
| Job not found, jobs query failed or denied, or the wait timed out | Exit code `8` (inconclusive). The outcome is unknown, so the command never reports success. The reindex was started; check it with `cratis chronicle jobs get <JOB_ID>`. |

The command polls at first every second, backing off to every five seconds. Waiting stops after 30 minutes, or when you cancel the command. Cancelling stops the wait, not the job, and the command prints the job ID with exit code `8`. A transient connection or gRPC error while polling is treated the same way: the job ID is printed and nothing is resubmitted.

Reliable `--wait` needs a Chronicle kernel of version 19.196.0 or later, which keeps successful reindex jobs until the normal job retention. On an older kernel a successful job can disappear before the CLI sees it, and the command then reports that it cannot verify the outcome.
