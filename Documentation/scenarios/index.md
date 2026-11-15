---
title: Cratis CLI scenarios for Chronicle troubleshooting
description: Use the Cratis CLI to check appended Chronicle events, recover a failed observer partition, or replay a projection after changing a read model.
---

<a id="scenarios"></a>

Use these guides when a Cratis Chronicle read model is stale, an observer has failed, or you need to
check what a command wrote. Each guide follows one task through the commands and the result to check.

If you do not yet know where the problem is, start by verifying the events. An event in the log gives
you something concrete to trace through the observer that should have processed it.

| Scenario | When you need it |
| --- | --- |
| [Find and fix a stuck observer](./fix-a-stuck-observer.md) | A projection, reducer, or reactor has stopped advancing — a read model looks stale or a side effect never fired. |
| [Replay a projection](./replay-a-projection.md) | You changed how a read model is built and need to rebuild it from history. |
| [Verify events were appended](./verify-events-were-appended.md) | You want to confirm a command actually wrote the events you expected — debugging or auditing. |

Before inspecting or changing Chronicle state, check your [context](../context/index.md) so the
commands address the intended server, event store and namespace. The verification guide only reads
events; the recovery guides explain when retry or replay changes observer state.

For CLI installation and your first connection, use [Getting started](/cli/getting-started/).
