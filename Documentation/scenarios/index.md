# Scenarios

The command reference tells you what each `cratis` command does. This section is the other half: the
**tasks** you actually reach for the CLI to do, start to finish. Each scenario is a short recipe —
a goal, the commands in order, and how to know you're done.

| Scenario | When you need it |
| --- | --- |
| [Find and fix a stuck observer](./fix-a-stuck-observer.md) | A projection, reducer, or reactor has stopped advancing — a read model looks stale or a side effect never fired. |
| [Replay a projection](./replay-a-projection.md) | You changed how a read model is built and need to rebuild it from history. |
| [Verify events were appended](./verify-events-were-appended.md) | You want to confirm a command actually wrote the events you expected — debugging or auditing. |

New to the CLI? Start with [Getting started](/cli/getting-started/), then come back here when you have a
job to do.
