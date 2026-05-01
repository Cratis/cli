# Applications

Applications are OAuth clients authorized to connect to Chronicle. They represent non-human principals — services, workers, or CI pipelines — that authenticate using client credentials rather than a username and password. Applications are distinct from users: a user logs in interactively, while an application uses a client ID and secret to obtain tokens programmatically.

## list

Lists all OAuth client applications registered on the Chronicle server.

```bash
cratis chronicle applications list
```

### Examples

List all registered applications:

```bash
cratis chronicle applications list
```

List in plain format:

```bash
cratis chronicle applications list --output plain
```

## add

Registers a new OAuth client application with the specified client ID and secret.

```bash
cratis chronicle applications add <CLIENT_ID> <CLIENT_SECRET>
```

### Arguments

| Argument | Description |
|---|---|
| `CLIENT_ID` | The client identifier for the application. |
| `CLIENT_SECRET` | The client secret the application uses to authenticate. |

### Examples

Register a new application:

```bash
cratis chronicle applications add my-service my-secret-value
```

### Security note

Avoid passing secrets as shell arguments in environments where command history is logged. Use environment variables or a secrets manager to supply the secret value, then pass it via a variable:

```bash
cratis chronicle applications add my-service "$APP_SECRET"
```

## remove

Removes a registered application from Chronicle. This operation is irreversible. Any service using the removed application's credentials will immediately lose access.

```bash
cratis chronicle applications remove <APP_ID>
```

The command prompts for confirmation before proceeding. Pass `--yes` to skip the prompt in automated workflows.

### Arguments

| Argument | Description |
|---|---|
| `APP_ID` | The GUID of the application to remove. Use `applications list` to retrieve application IDs. |

### Options

| Flag | Description |
|---|---|
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Remove an application interactively:

```bash
cratis chronicle applications remove a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Remove without confirmation:

```bash
cratis chronicle applications remove a1b2c3d4-e5f6-7890-abcd-ef1234567890 --yes
```
