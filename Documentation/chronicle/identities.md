# Identities

An identity is a principal that has interacted with an event store. Chronicle captures identity information from the context metadata of every appended event, recording the user, application, or service responsible for each write. The identities list gives you a complete picture of which principals have ever produced events in the event store.

## list

Lists all identities recorded in the specified event store and namespace.

```bash
cratis chronicle identities list
```

### Options

| Flag | Description |
|---|---|
| `-e, --event-store <NAME>` | Event store to inspect. Defaults to `default`. |
| `-n, --namespace <NAME>` | Namespace to inspect. Defaults to `Default`. |

### Output tip

Use `--output plain` when scanning identity names or piping into other commands. The plain format returns one identity per line and is significantly more compact than the JSON output.

```bash
cratis chronicle identities list --output plain
```

### Examples

List all identities in the default event store:

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
