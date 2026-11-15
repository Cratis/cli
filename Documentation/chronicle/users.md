---
title: Manage Chronicle users with Cratis CLI
description: Use Cratis CLI to list, add and remove Chronicle user accounts for human operators who authenticate with a username and password.
---

<a id="users"></a>

Users are human principals who can log in to Chronicle using a username and password. They are distinct from applications, which authenticate with client credentials. Manage users when you need to grant or revoke interactive access to the Chronicle server.

## Administrator login

Listing, adding and removing users needs a Chronicle administrator. Sign in as one first, then run the command:

```bash
cratis chronicle login admin
cratis chronicle users list
```

An application's client credentials, including the development client, are refused. Chronicle then answers with this hint:

```text
This command needs a Chronicle administrator. Run 'cratis chronicle login <administrator-username>' to sign in as one.
```

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

A variable can keep the literal password out of the command recorded in shell history, but shell expansion still places the password in the process arguments, where process inspection or argument logging may expose it:

```bash
cratis chronicle users add alice alice@example.com "$INITIAL_PASSWORD"
```

## remove

Removes the user account registration from Chronicle. This operation is irreversible. Do not treat removal as a guarantee that all previously issued tokens are immediately revoked.

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
