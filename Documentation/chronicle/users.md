# Users

Users are human principals who can log in to Chronicle using a username and password. They are distinct from applications, which authenticate with client credentials. Manage users when you need to grant or revoke interactive access to the Chronicle server.

## list

Lists all users registered on the Chronicle server.

```bash
cratis chronicle users list
```

### Examples

List all users:

```bash
cratis chronicle users list
```

List in plain format:

```bash
cratis chronicle users list --output plain
```

## add

Creates a new user account with the specified username, email address, and password.

```bash
cratis chronicle users add <USERNAME> <EMAIL> <PASSWORD>
```

### Arguments

| Argument | Description |
|---|---|
| `USERNAME` | The login name for the new user. |
| `EMAIL` | The email address for the new user. |
| `PASSWORD` | The initial password for the new user. |

### Examples

Add a new user:

```bash
cratis chronicle users add alice alice@example.com initialpassword
```

### Security note

Avoid passing passwords as shell arguments in environments where command history is logged. Use a variable to supply the password:

```bash
cratis chronicle users add alice alice@example.com "$INITIAL_PASSWORD"
```

## remove

Removes a user account from Chronicle. This operation is irreversible. The user immediately loses all access to the server.

```bash
cratis chronicle users remove <USER_ID>
```

The command prompts for confirmation before proceeding. Pass `--yes` to skip the prompt in automated workflows.

### Arguments

| Argument | Description |
|---|---|
| `USER_ID` | The GUID of the user to remove. Use `users list` to retrieve user IDs. |

### Options

| Flag | Description |
|---|---|
| `-y, --yes` | Skip confirmation prompt. |

### Examples

Remove a user interactively:

```bash
cratis chronicle users remove a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Remove without confirmation:

```bash
cratis chronicle users remove a1b2c3d4-e5f6-7890-abcd-ef1234567890 --yes
```
