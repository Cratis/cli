# Auth

Chronicle supports two authentication modes:

- **User login** — a human operator authenticates with a username and password. The CLI caches the resulting token for subsequent requests.
- **Client credentials** — an application identity authenticates using a client ID and client secret. Configure these values on the active context using `cratis context set-value client-id` and `cratis context set-value client-secret`.

When no authentication is configured, Chronicle commands use the built-in development client credentials. Non-local servers will normally reject them.

## auth status

Shows the current authentication state for the active context: the logged-in user, whether client credentials are configured, or that no authentication is active. A user session includes `Bound to: <server>` and a warning if the stored login does not match the selected server. The selected server follows the normal flag, environment variable, context, and default resolution order.

JSON output includes `tokenServer` (the server bound to the login) and `loginMatchesServer` (`true` or `false`, or `null` when there is no stored login). SRV and multiple-host selections do not match a stored login.

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

Authenticates as a user using the resource owner password credentials flow. The CLI stores the access token and expiry in the active context (`~/.cratis/config.json`) for use by subsequent commands. It does not create a token file for user login. The token is bound to the server that issued it. You can log in to a different server with `--server`, but the active context keeps its own server setting. If the server returns no usable token or expiry, login fails without changing the context.

```bash
cratis chronicle login <USERNAME>
```

If you omit `--secret`, the CLI prompts for the password interactively so it does not appear in your shell history.

Login uses the same TLS configuration as every other Chronicle command. `certificatePath` supplies a PKCS#12 client certificate with its private key, with or without a password; set `certificatePassword` when needed. It is not a PEM or DER custom trust root. With `skipTlsValidation=false`, the CLI accepts a valid server certificate, or a certificate with only chain errors whose hash matches the configured client certificate. A name mismatch is still rejected. With `skipTlsValidation=true` (the default), server certificate validation is skipped. A missing or invalid client certificate fails login without storing a session.

### Arguments

| Argument | Description |
|---|---|
| `USERNAME` | The username to authenticate as. |

### Options

| Flag | Description |
|---|---|
| `--secret <PASSWORD>` | The password. Omit to be prompted interactively. |
| `--server <CONNECTION_STRING>` | Log in to this single, direct Chronicle server instead of the active context's server. The stored token can be used only with this server. SRV and multiple-host login addresses are not supported. |

### Examples

Log in interactively (password prompt):

```bash
cratis chronicle login alice
```

Log in with password inline (use only in controlled automation):

```bash
cratis chronicle login alice --secret mysecret
```

Subsequent Chronicle commands use the active context's token only for the server that issued it, including a matching `--server` override. If you logged in with an override that differs from the active context's server, commands without that override target the context's server and do not use the token. For a different server, the token is never sent; the command uses the normal no-login credentials. If the token expires, explicit client credentials on the context take over; otherwise, run `cratis chronicle login` again. Use a separate context if you need to keep a different server as the default.

After you change the context's server, or select a different server with a flag or environment variable, non-workbench commands print a notice to stderr once per process:

```text
Note: the stored login for alice belongs to production:35000; this command connects to staging:35000 without it. Run 'cratis chronicle login alice' to log in to this server.
```

The notice does not require `--debug` and leaves JSON on stdout unchanged. Run `cratis chronicle auth status` to compare the bound and selected servers, then log in to the selected server if needed. The workbench keeps its own panel messaging instead of writing this notice over the terminal UI.

Older CLI versions could record a username without a token. Such legacy contexts fall back to the development client and print a warning to stderr asking you to log in again; machine-readable output remains unchanged.

`~/.cratis/config.json` contains user login tokens. The Chronicle client writes its own files under `~/.cratis/tokens` for client-credential caching. On Unix, the CLI restricts `~/.cratis` to mode 0700 and its config file to 0600 when saving; on Windows, access relies on the user profile ACL.

### Note

`login` is a top-level `chronicle` command, not a sub-command of `auth`. The full command is `cratis chronicle login`, not `cratis chronicle auth login`.

## logout

Clears the cached credentials and token for the active context. Subsequent commands fall back to the built-in development client credentials, which non-local servers will normally reject.

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
