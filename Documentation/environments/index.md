---
title: Configure project environments with the Cratis CLI
description: Cratis CLI environments keep named project configuration and secret references beside your model, with a local selection for each user.
---

<a id="environments"></a>

Cratis CLI environments hold the configuration that changes between deployments of a project. Your
Cratis Screenplay model can stay the same while a client ID or SMTP host differs between development
and production. Each named environment keeps those values in a file beside the model.

Create the environments you need, select one, then set its values:

```bash
cratis env init                      # creates "development"
cratis env init production           # creates "production"
cratis env select production
cratis config set auth:clientId my-client
echo -n "$SMTP_PASSWORD" | cratis config set smtp:password --secret
cratis config
```

The values set here belong to `production`, because it was selected before the `config` commands.
`--secret` reads the password from standard input instead of putting it in a command argument. These
are example key names; use [Missing keys](#missing-keys) to find the keys your model requires.

## Files

Each environment is stored in `.cratis/environments/<name>.yaml`, a file intended to be committed with
the project. Names contain letters, digits, `.`, `_` and `-` and must start with a letter or digit;
examples include `development`, `staging` and `customer-x`.

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

`cratis config --missing <path>` compares the keys a model needs with the environment and exits with
`5` when any are missing. `<path>` is a Screenplay model (a `.play` file or a folder of them) or a key list.

For a model, the keys are the ones Screenplay derives from its declarations: `authentication.providers.<Identity>.clientId` and `.clientSecret` for every identity provider (plus `.tenantId` for `EntraId` and `.authority` for `OpenId`), and `onboarding.email.sender` and `onboarding.email.server.host`, `.port`, `.username` and `.password` when the model declares `onboarding`. The convention is Screenplay's, in its environments reference.

A model that does not compile exits with `5` and names the first errors: the CLI cannot list required
keys until it can understand the model. A key list is a JSON array of strings, or one key per line
(blank lines and `#` comments ignored).

`cratis run` reads the same keys: it resolves the environment before it starts Docker and fails the same way when they are missing. See [Run](../reference/run.md#authentication-and-onboarding).

## Reading environments from code

The `Cratis.Environments` NuGet package reads the same files, so Stage, AuthProxy tooling and other tools resolve values and secrets the same way.

```csharp
var environments = ProjectEnvironments.Discover(Directory.GetCurrentDirectory());
var selected = environments.Resolve(explicitName: null);
var environment = ProjectEnvironment.Open(environments, selected.Name, PassphraseSources.FromEnvironment());

var clientId = await environment.GetValueAsync("auth:clientId");
var smtpPassword = await environment.GetValueAsync("smtp:password"); // decrypted or fetched from the vault
```

For the passphrase and vault setup behind secret values, continue with [Secrets](secrets.md). If you
are configuring a model to run locally, [Run](../reference/run.md#authentication-and-onboarding)
explains how the runtime uses its environment values.
