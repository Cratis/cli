---
title: List Chronicle identities with Cratis CLI
description: Use Cratis CLI to list recorded event identities in a Chronicle event store and namespace, including subject, name and username.
---

<a id="identities"></a>

An identity records event identity metadata in an event store and namespace. The identities list is scoped to the selected event store and namespace, not the entire event store. For application callers, `CausedBy` can be caller-supplied identity metadata rather than independently authenticated end-user attribution; for authenticated Chronicle user callers, Chronicle derives it from the caller's identity.

## list

Lists all identities recorded in the specified event store and namespace.

```bash
cratis chronicle identities list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to the active context's event store, then `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to the active context's namespace, then `Default`. |

### Output tip

Use `--output plain` when scanning identity names or piping into other commands. The plain format returns one identity per line and is significantly more compact than the JSON output.

```bash
cratis chronicle identities list --output plain
```

### Examples

List identities using the active context's event store and namespace, falling back to `default` and `Default`:

```bash
cratis chronicle identities list
```

List identities in a specific event store and namespace:

```bash
cratis chronicle identities list -e myapp -n tenant-a
```

Get identity values for scripting:

```bash
cratis chronicle identities list -q
```
