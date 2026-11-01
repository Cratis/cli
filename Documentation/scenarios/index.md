---
title: Scenarios
description: Bounded CLI workflows that connect inspection evidence, ownership, and explicit recovery decisions.
---

Use these workflows when you have a concrete question, rather than running the
command reference top to bottom. Complete [first inspection](../getting-started/index.mdx),
confirm an authorized target, and set a request/time budget before investigating.

| Scenario | When you need it |
| --- | --- |
| [Find a missing projection](find-a-missing-projection.md) | Distinguish store registration, namespace runtime state, and read-model ownership. |
| [Verify events were appended](verify-events-were-appended.md) | Inspect a small event interval for the expected source and type. |
| [Find and fix a stuck observer](fix-a-stuck-observer.md) | Gather failure evidence and approve a targeted recovery after fixing the cause. |
| [Replay a projection](replay-a-projection.md) | Rebuild invalid state with explicit approval of the exact target and effects. |

A successful empty response is not an unavailable check. Neither alone calls for
recovery. These procedures separate read-only diagnosis from retries, quarantine
changes, replay, and recommendation execution; each state change needs its own
authorization. For application endpoint metadata instead, use
[Arc discovery](../arc/index.md), which targets a separate HTTP application.
