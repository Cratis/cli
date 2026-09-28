# Auth

Chronicle supports two authentication modes:

- **User login** — a human operator authenticates with a username and password. The CLI caches the resulting token for subsequent requests.
- **Client credentials** — an application identity authenticates using a client ID and client secret. Configure these values on the active context using `cratis context set-value client-id` and `cratis context set-value client-secret`.

When no authentication is configured, local Chronicle commands use the built-in development client credentials.

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

Authenticates as a user using the resource owner password credentials flow. The CLI stores the access token and expiry in the active context (`~/.cratis/config.json`) for use by subsequent commands. It does not create a token file for user login. The token is bound to the server that issued it; a login targeting a different server than the active context is refused. If the server returns no usable token or expiry, login fails without changing the context.

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
| `--server <CONNECTION_STRING>` | Connect to this server for login only if it matches the active context's server. To use another server, create a context with `cratis context create <name> --server <url>` and switch with `cratis context set <name>`. |

### Examples

Log in interactively (password prompt):

```bash
cratis chronicle login alice
```

Log in with password inline (use only in controlled automation):

```bash
cratis chronicle login alice --secret mysecret
```

Subsequent Chronicle commands use the active context's token only for the server that issued it, including a matching `--server` override. For a different server, the token is never sent; the command uses the normal no-login credentials. If the token expires, run `cratis chronicle login` again. Switch contexts to use a different server.

Older CLI versions could record a username without a token. Such legacy contexts fall back to the development client and print a warning to stderr asking you to log in again; machine-readable output remains unchanged.

`~/.cratis/config.json` contains user login tokens. The Chronicle client writes its own files under `~/.cratis/tokens` for client-credential caching. On Unix, the CLI restricts `~/.cratis` to mode 0700 and its config file to 0600 when saving; on Windows, access relies on the user profile ACL.

### Note

`login` is a top-level `chronicle` command, not a sub-command of `auth`. The full command is `cratis chronicle login`, not `cratis chronicle auth login`.

## logout

Clears the cached credentials and token for the active context. Subsequent local commands fall back to the built-in development client credentials.

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
