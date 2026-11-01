---
description: Named project environments in .cratis/environments - development, production or any name - with config values, selection per user and the cratis env and cratis config commands.
---

# Environments

A Screenplay model is the same in every environment. What it needs to run - a client ID, an SMTP host, a signing key - is not. If you have used Pulumi stacks, the idea is the same: a project has named **environments**, and each one carries its own configuration next to the model.

```bash
cratis env init                      # creates "development"
cratis env init production           # creates "production"
cratis env select production
cratis config set auth:clientId my-client
echo -n "$SMTP_PASSWORD" | cratis config set smtp:password --secret
cratis config
```

## Files

Each environment is one committed file, `.cratis/environments/<name>.yaml`. A name is any safe file name: letters, digits, `.`, `_` and `-`, starting with a letter or digit - `development`, `staging`, `customer-x`.

```yaml
secretsProvider: passphrase
encryptionSalt: <base64>
encryption:
  kdf: pbkdf2-sha256
  iterations: 600000
  check: <base64>
config:
  auth:clientId: my-client
  smtp:host: mail.example.com
  smtp:password:
    secure: <base64>
  signing:key:
    ref: <vault reference>
```

| Entry | Meaning |
|---|---|
| `key: value` | A plain value. |
| `key: { secure: <base64> }` | A secret encrypted by the passphrase provider. Safe to commit. |
| `key: { ref: <name> }` | A secret kept in Azure Key Vault or HashiCorp Vault. The file holds only the reference. |

`secretsProvider`, `encryptionSalt` and `encryption` describe where secrets live; see [Secrets](secrets.md).

## Selecting an environment

The environment in effect is, in order of precedence:

1. `--environment <name>` (or `-e <name>` on `env` and `config` commands) on the command line
2. the `CRATIS_ENVIRONMENT` environment variable
3. your selection, made with `cratis env select <name>`
4. `development`

The selection is per user and is not committed: it is written to `.cratis/local/selected-environment`, and `cratis init` (and `cratis env select`) add `.cratis/local/` to `.gitignore`.

> [!NOTE]
> On Chronicle commands `-e` already means `--event-store`. Use the long `--environment` there.

## Commands

| Command | Description |
|---|---|
| `cratis env init [name]` | Create an environment; `development` without a name. `--secrets-provider` picks where secrets are kept. |
| `cratis env select <name>` | Select the environment for this user. |
| `cratis env ls` | List environments; the one in effect is marked. |
| `cratis env rm <name>` | Remove an environment (asks to confirm; `--yes` skips). |
| `cratis config set <key> <value>` | Set a plain value. |
| `cratis config set <key> --secret` | Set a secret, read from stdin or a hidden prompt. |
| `cratis config get <key>` | Print a value; a secret prints as `[secret]` unless `--show-secret`. |
| `cratis config rm <key>` | Remove a value; a secret in a vault is deleted from the vault too. |
| `cratis config` | List the values; secrets show as `[secret]`. |
| `cratis config --missing <path>` | List the keys a model needs that the environment lacks. |

All of them accept `--output json` and use the CLI exit codes: `0` success, `1` not found, `4` authentication failed (wrong passphrase), `5` validation (including missing keys for `--missing`).

### Missing keys

`cratis config --missing <path>` compares the keys a model needs with the environment and exits with `5` when any are missing. `<path>` is a key list today: a JSON array of strings, or one key per line (blank lines and `#` comments ignored). Reading the keys straight from a Screenplay model arrives with the key binding convention in Screenplay; the command already takes its keys from a pluggable source, so no flag changes when it does.

## Reading environments from code

The `Cratis.Environments` NuGet package reads the same files, so Stage, AuthProxy tooling and other tools resolve values and secrets the same way.

```csharp
var environments = ProjectEnvironments.Discover(Directory.GetCurrentDirectory());
var selected = environments.Resolve(explicitName: null);
var environment = ProjectEnvironment.Open(environments, selected.Name, PassphraseSources.FromEnvironment());

var clientId = await environment.GetValueAsync("auth:clientId");
var smtpPassword = await environment.GetValueAsync("smtp:password"); // decrypted or fetched from the vault
```
