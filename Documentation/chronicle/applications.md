---
title: Manage Chronicle applications with Cratis CLI
description: List, add and remove the OAuth client applications that authenticate to Chronicle with client credentials, using cratis chronicle applications.
---

<a id="applications"></a>

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

A variable can keep the literal secret out of the command recorded in shell history, but shell expansion still places the secret in the process arguments, where process inspection or argument logging may expose it. Supplying the value from an environment variable or secrets manager does not remove that exposure:

```bash
cratis chronicle applications add my-service "$APP_SECRET"
```

## remove

Removes the application's credential registration from Chronicle. This operation is irreversible. Do not treat removal as a guarantee that all previously issued tokens are immediately revoked.

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
