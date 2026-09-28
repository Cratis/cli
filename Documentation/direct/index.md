# Direct login

The Direct CLI signs you in as yourself through an external browser. This login is independent of Chronicle contexts and does not give access to Chronicle commands. Direct's authorization server must register `cratis-cli` as a public client with authorization code, PKCE S256, refresh tokens, loopback redirect URIs with ephemeral ports, and revocation enabled. There is no client secret.

```bash
cratis direct login --tenant your-tenant
cratis direct status
cratis direct use another-tenant
cratis direct logout --tenant your-tenant
cratis direct logout --all
```

## Login

`cratis direct login [--url ORIGIN] [--tenant TENANT] [--issuer ISSUER] [--insecure-file-store]` opens your browser and waits up to three minutes for the callback at `http://127.0.0.1:<ephemeral-port>/callback`. If the browser cannot be launched, copy the authorization URL printed to stderr into your browser. The CLI validates the callback's exact address, state and `iss` (RFC 9207), then exchanges the code using PKCE S256. A callback with the wrong address, state or issuer is refused and the CLI keeps waiting. If a callback with the correct state and issuer carries an OAuth error, for example because you declined consent or chose a tenant you do not belong to, the login ends immediately. The CLI reports the error code (`access_denied`, `invalid_scope`, `invalid_request`, `server_error`, `temporarily_unavailable`, `unauthorized_client`, `consent_required`, `login_required`, or `unknown` for anything else) with guidance; the error description sent by the server is not shown.

`--url` is the Direct HTTPS origin, defaulting to `https://cratis.direct` (or the origin used for the previous Direct login). The protected resource is `<origin>/mcp`. The CLI looks for `<origin>/.well-known/oauth-protected-resource/mcp` (RFC 9728) to discover the authorization server. If that metadata is absent, pass its HTTPS `--issuer`. When metadata is present, a supplied issuer must be listed there. The server's endpoints are discovered from RFC 8414 `/.well-known/oauth-authorization-server`, with a fallback to `/.well-known/openid-configuration` only when the former returns 404. Requests carry `resource=<origin>/mcp` in both authorization and token exchanges, and request `direct:read direct:content.write direct:work` plus offline refresh-token support from the server. `--tenant` is an authorization-server **hint**, not proof of tenant membership; the server must validate tenant access and put the selected tenant into the issued token.

By default, access and refresh tokens are stored per origin, resource, and tenant in macOS Keychain, Windows Credential Manager, or Linux libsecret (`secret-tool`). An unavailable keychain fails the login; there is no automatic plaintext fallback. On Linux, `secret-tool` comes from the `libsecret-tools` package and needs an unlocked Secret Service keyring; credentials are passed to it on standard input, never as arguments. `--insecure-file-store` explicitly opts in to separate `~/.cratis/direct-secrets/` files with 0600 permissions on Unix. This choice is remembered for that origin and tenant in `~/.cratis/config.json`; only the origin, tenant, issuer and store choice are recorded there, never Direct tokens. The plaintext fallback is unavailable on Windows. Access-token refresh is serialized across processes using a lock under `~/.cratis/direct-locks/`.

## Status, switching tenants and logout

`cratis direct status` refreshes an expiring token and asks `<origin>/.cratis/me` for the user. It displays the user, selected tenant, granted scopes (when included in the token response), and access-token expiry. A 401 asks you to log in again. The displayed tenant falls back to the locally selected tenant hint when the identity response has no tenant field; it is not a server-verified tenant in that case. Status also lists every stored Direct credential by origin, tenant and store (`os` for the OS credential manager, `file` for the plaintext store), marking the active one. Only this non-secret metadata is shown. If the active origin and tenant have no credential, status fails and says how many other credentials are stored.

`cratis direct use <TENANT>` does **not** relabel the previous token. It runs the browser authorization flow again for the new tenant and makes it the active one. The previous tenant's credential stays in its own store entry: it is listed by `status` and can be revoked with `cratis direct logout --tenant <TENANT>` or `--all`. Running `use` or `login` again for the same origin and tenant replaces that credential.

Replacing a stored credential revokes its refresh token at the issuer that granted it before the new one is saved. This is best effort: if revocation fails, the CLI prints a warning without any token contents, removes the old credential locally and completes the login. A refresh token that could not be revoked stays valid until it expires or is revoked at the authorization server.

`cratis direct logout` calls the issuer's revocation endpoint with the refresh token before deleting the local entry. If revocation fails, it reports an error and retains the credential instead of falsely claiming a successful logout. Without options, logout addresses the active origin and tenant. `--tenant <TENANT>` addresses that tenant's credential on the active origin (or on `--url`). `--all` revokes and deletes every stored Direct credential on every origin; combine it with `--url` to limit it to one origin. `--all` and `--tenant` cannot be combined. With `--all`, logout continues past a failed revocation, retains that credential, and exits with an error naming the credentials that remain. Supply `--insecure-file-store` to these commands only if you opted into plaintext storage and the stored configuration no longer records it. Tokens and refresh-token contents are never printed.

The CLI keeps the non-secret index of stored credentials (origin, tenant, issuer and store choice) in `~/.cratis/config.json`. A credential stored outside that index is not listed or addressed by `status` and `logout --all`.

The future `cratis direct mcp` bridge will request fresh resource-bound access tokens from the internal Direct token provider. This command branch does not yet implement MCP transport.
