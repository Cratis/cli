# Auth

Chronicle supports two authentication modes:

- **User login** — a human operator authenticates with a username and password. The CLI caches the resulting token for subsequent requests.
- **Client credentials** — an application identity authenticates using a client ID and client secret. Configure these values on the active context using `cratis context set-value client-id` and `cratis context set-value client-secret`.

When no authentication is configured or required, commands connect anonymously.

## auth status

Shows the current authentication state for the active context: the logged-in user, whether client credentials are configured, or that no authentication is active.

```bash
cratis chronicle auth status
```

### Examples

Check authentication status:

```bash
cratis chronicle auth status
```

Get machine-readable status:

```bash
cratis chronicle auth status -o json
```

## login

Authenticates as a user using the resource owner password credentials flow. The CLI stores the access token and expiry in the active context (`~/.cratis/config.json`) for use by subsequent commands. It does not create a token file for user login. If the server returns no usable token or expiry, login fails without changing the context.

```bash
cratis chronicle login <USERNAME>
```

If you omit `--secret`, the CLI prompts for the password interactively so it does not appear in your shell history.

### Arguments

| Argument | Description |
|---|---|
| `USERNAME` | The username to authenticate as. |

### Options

| Flag | Description |
|---|---|
| `--secret <PASSWORD>` | The password. Omit to be prompted interactively. |
| `--server <CONNECTION_STRING>` | Connect to this server for login; save the token in the active context, even when this server differs from that context's server. |

### Examples

Log in interactively (password prompt):

```bash
cratis chronicle login alice
```

Log in with password inline (use only in controlled automation):

```bash
cratis chronicle login alice --secret mysecret
```

Subsequent Chronicle commands use the active context's token, including when you pass the same `--server` override. If the token expires or is missing, they report a login error instead of falling back to local development credentials; run `cratis chronicle login` again. Because `--server` overrides only the endpoint, switch contexts before logging in if you want a different context to own the token.

### Note

`login` is a top-level `chronicle` command, not a sub-command of `auth`. The full command is `cratis chronicle login`, not `cratis chronicle auth login`.

## logout

Clears the cached credentials and token for the active context. Subsequent commands will connect anonymously unless client credentials are configured on the context.

```bash
cratis chronicle logout
```

### Examples

Log out of the current context:

```bash
cratis chronicle logout
```

### Note

`logout` is a top-level `chronicle` command, not a sub-command of `auth`. The full command is `cratis chronicle logout`, not `cratis chronicle auth logout`.
