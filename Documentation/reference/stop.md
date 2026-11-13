---
title: Stop Stage sandboxes with Cratis CLI
description: Stop a Stage sandbox that cratis run left running with --detach, by name or all at once, using cratis stop.
---

`cratis stop` stops a Stage sandbox that [`cratis run`](run.md) started — typically one left running with `--detach`.

```bash
cratis stop [NAME] [--all]
```

```bash
cratis stop
cratis stop cratis-stage-a1b2c3d4
cratis stop --all
```

## Arguments

| Argument | Description |
|---|---|
| `NAME` | The sandbox to stop, as `cratis run --detach` reported it. It must start with `cratis-stage-`, so no other container can be stopped by mistake. Optional when exactly one sandbox is running. |

## Options

| Option | Description |
|---|---|
| `--all` | Stop every running Stage sandbox. |

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## Output

```json
{ "status": "stopped", "stopped": ["cratis-stage-a1b2c3d4"] }
```

## Errors

| Condition | Result |
|---|---|
| No name and no sandbox is running | Not found (exit code `1`). |
| No name and several sandboxes are running | Validation error (exit code `5`) listing them. Name one, or pass `--all`. |
| The name does not start with `cratis-stage-`, or is combined with `--all` | Validation error (exit code `5`). |
| `docker` is not installed or not running | Connection error (exit code `2`). |
| A container is still running after 30 seconds | Server error (exit code `3`). |
