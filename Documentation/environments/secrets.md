---
title: Store, read and rotate secrets with the Cratis CLI
description: Use the Cratis CLI to store and read environment secrets, replace a secret value, change a passphrase or move secrets to a vault.
---

<a id="secrets"></a>

The Cratis CLI stores environment secrets as encrypted values or vault references when you use `--secret`. Use it for passwords and signing keys; values saved without this flag remain plain text.

Each [environment](index.md) has a secrets provider, which determines where its secrets are stored. Choose one when creating the environment: passphrase encryption is the default, or you can use Azure Key Vault or HashiCorp Vault. The following commands are alternatives; run one to create `production`.

```bash
cratis env init production                                                    # passphrase (default)
cratis env init production --secrets-provider azurekeyvault://my-vault
cratis env init production --secrets-provider hashivault://vault.example.com:8200/secret/cratis
```

<a id="setting-and-reading-secrets"></a>

## Store a secret

Before setting a value, [select the environment](index.md#selecting-an-environment) you want to change, or pass `-e production` to each `config` command below. Creating an environment does not select it.

Use `cratis config set` with `--secret`. The command rejects a value passed as an argument with this flag. Instead, pipe the value from an existing environment variable, or type it at the hidden prompt:

```bash
echo -n "$SMTP_PASSWORD" | cratis config set smtp:password --secret
cratis config set smtp:password --secret        # prompts, hidden
```

These are two ways to set the same key. Avoid typing a literal secret into the shell command: that text can be recorded in shell history even if the CLI reads it through a pipe. The command reports the key and environment after storing the value, without echoing the secret.

## Read a secret

`cratis config get` shows `[secret]` by default. Add `--show-secret` when you need the actual value:

```bash
cratis config get smtp:password                 # [secret]
cratis config get smtp:password --show-secret   # the value
```

Without the flag, `get` does not decrypt or fetch the secret. Listing configuration with `cratis config` also shows `[secret]` without resolving it. With `--show-secret`, the value appears in command output, including JSON output if selected, so keep that output out of shared logs.

## Rotate a secret

To replace one stored password or key, run `cratis config set smtp:password --secret` again and supply the new value. This updates the environment's stored value; it does not change the password on the SMTP server. Coordinate that change with the service that issues or accepts the credential.

If you need to change the passphrase protecting all secrets instead, use [Changing the passphrase](#changing-the-passphrase). To move secrets between storage providers, use [Changing the provider](#changing-the-provider).

## Passphrase provider

The default provider encrypts secrets in the environment file with AES-256-GCM. It derives the encryption key from your passphrase using PBKDF2-SHA256 and a random salt, recorded as `encryptionSalt`. Each environment gets its own salt. The file also records the key derivation algorithm and iteration count, which defaults to 600,000.

Each encrypted value is stored as `secure: <base64>`, containing a nonce, authentication tag and ciphertext. Encryption binds the value to its configuration key, so copying the ciphertext to a different key fails authentication. A separate verification value detects a wrong passphrase and returns exit code `4` before decrypting a secret value.

For an existing environment, the CLI reads the passphrase from `CRATIS_PASSPHRASE`, then the file named by `CRATIS_PASSPHRASE_FILE`, then a hidden prompt, using the first available source. It does not save the passphrase in the environment file. Before committing that file, review any plain-text configuration entries and keep the passphrase out of the repository.

### Changing the passphrase

```bash
cratis env change-passphrase -e production
```

This re-encrypts every secret with a new passphrase and salt, without changing the secret values themselves. The current passphrase is needed to decrypt the existing values. Supply the new one through `CRATIS_NEW_PASSPHRASE`, the file named by `CRATIS_NEW_PASSPHRASE_FILE`, or a hidden prompt, in that order. At the prompt, enter it twice to confirm.

## Azure Key Vault provider

Use `azurekeyvault://<vault>` to keep the secret values in Azure Key Vault and only their names in the environment file as `ref:` entries. The vault target can be a name (`my-vault` means `https://my-vault.vault.azure.net`), a full vault host or a URL.

The provider builds secret names from `cratis-<environment>-<key>`, replaces characters other than ASCII letters and digits with dashes, and adds a short hash suffix. Authentication uses `DefaultAzureCredential`, which supports Azure CLI login, managed identity and workload identity. Configure credentials with access to the vault before storing or reading a secret.

## HashiCorp Vault provider

Use `hashivault://<address>/<mount>[/<path>]`, for example `hashivault://vault.example.com:8200/secret/cratis`, for HashiCorp Vault. The mount must use KV v2, Vault's versioned key-value secrets engine. The provider stores each secret at `<path>/<environment>/<key>` within that mount; the path defaults to `cratis`. The environment file holds that path as `ref:`.

Connections use HTTPS by default. `hashivault+http://` selects unencrypted HTTP for a development server.

Configure authentication through environment variables. The provider checks token, AppRole and Kubernetes authentication in that order:

| Method | Variables |
|---|---|
| Token | `VAULT_TOKEN` |
| AppRole | `VAULT_ROLE_ID`, `VAULT_SECRET_ID` |
| Kubernetes | `VAULT_K8S_ROLE`, and the service account token from `VAULT_K8S_TOKEN_FILE` (default `/var/run/secrets/kubernetes.io/serviceaccount/token`) |

`VAULT_AUTH_MOUNT` overrides the auth method mount and `VAULT_NAMESPACE` sets the Vault namespace.

## Changing the provider

```bash
cratis env change-secrets-provider azurekeyvault://my-vault -e production
```

Make sure you can read from the current provider and write to the target provider before running this command. The CLI reads each secret through the current provider and stores it through the new one. Moving to a vault replaces `secure:` values with `ref:` entries; moving back to `passphrase` requires a new passphrase. Secret values are not written to the environment file in plain text during the move.

After saving the new file, the CLI requests deletion of old vault entries whose stored references changed. For Azure Key Vault, it starts deletion; it does not purge the secret.

Once the secrets are in place, [check for missing model keys](index.md#missing-keys) before running the application.
