# Verify events were appended

You ran a command — or a whole workflow — and you want to confirm it actually wrote the events you
expected. Maybe a read model didn't update and you're isolating whether the problem is the *write* or
the *projection*. The CLI reads straight from the event log, so you can check the source of truth
directly.

## 1. Check the tail moved

The tail is the sequence number of the last appended event. Capture it before and after an action to
confirm something was written:

```bash
cratis chronicle events tail --output plain
```

For a specific event source — say the account you just acted on:

```bash
cratis chronicle events tail --event-source-id "account-42"
```

## 2. Read the events

List the events for that event source and eyeball what landed:

```bash
cratis chronicle events get --event-source-id "account-42"
```

Filter to the event type you expected, and get the full document as JSON to inspect its content:

```bash
cratis chronicle events get --event-source-id "account-42" --event-type AccountOpened --output json
```

If the event you expected isn't there, the problem is on the **write** side — the command didn't append
it (a validation failure, a constraint, an unhandled path). If it *is* there but a read model is wrong,
the problem is on the **read** side — see [Find and fix a stuck observer](./fix-a-stuck-observer.md).

## 3. Audit a range

For a compliance check or a wider audit, read a range of the log and export it:

```bash
cratis chronicle events get --from 0 --to 500 --output json > audit.json
```

Use `--output plain` instead when you're scanning in the terminal — it's far smaller because it omits
metadata and schema details.

## Done when

You've confirmed the expected events exist (or proven they don't) for the event source in question, and
you know which side of the loop — write or read — to look at next.
