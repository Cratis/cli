---
description: Secrets providers per environment - passphrase encryption, Azure Key Vault and HashiCorp Vault - and the cratis config --secret, env change-secrets-provider and env change-passphrase commands.
---

# Secrets

Client secrets, SMTP passwords and signing keys never sit in an environment file in clear text. Like Pulumi, each [environment](index.md) chooses a **secrets provider** when it is created.

```bash
cratis env init production                                                    # passphrase (default)
cratis env init production --secrets-provider azurekeyvault://my-vault
cratis env init production --secrets-provider hashivault://vault.example.com:8200/secret/cratis
```

## Setting and reading secrets

A secret is never taken from a command-line argument, so it stays out of shell history. Pipe it in, or run the command in a terminal and type it at the hidden prompt.

```bash
echo -n "$SMTP_PASSWORD" | cratis config set smtp:password --secret
cratis config set smtp:password --secret        # prompts, hidden
cratis config get smtp:password                 # [secret]
cratis config get smtp:password --show-secret   # the value
```

No command prints a secret to stdout, stderr, a log or JSON output unless you pass `--show-secret`.

## Passphrase provider

The default. Secrets are encrypted inline with AES-256-GCM:

- The key is derived from your passphrase and a random **per-environment salt** (`encryptionSalt`) with PBKDF2-SHA256. The algorithm and iteration count (600,000 by default) are recorded in the file, so they can change later. Two environments with the same passphrase get different salts and different ciphertext.
- Each value is stored as `secure: <base64>` (nonce, tag and ciphertext) and is bound to its key, so moving a ciphertext to another key fails.
- A verification value in the file makes a wrong passphrase fail cleanly with exit code `4`, before anything is decrypted.

The passphrase is read from, in order, `CRATIS_PASSPHRASE`, the file named by `CRATIS_PASSPHRASE_FILE`, or a hidden prompt. It is never written anywhere. A file with only `secure:` values is safe to commit.

### Changing the passphrase

```bash
cratis env change-passphrase -e production
```

Every secret is decrypted with the current passphrase and re-encrypted under the new one with a new salt. The new passphrase comes from `CRATIS_NEW_PASSPHRASE`, `CRATIS_NEW_PASSPHRASE_FILE` or a prompt.

## Azure Key Vault provider

`azurekeyvault://<vault>` - a vault name (`my-vault` means `https://my-vault.vault.azure.net`) or a full vault host or URL. Values are stored in the vault as secrets named `cratis-<environment>-<key>` with a short hash suffix; the file holds the name as `ref:`. Authentication uses `DefaultAzureCredential`: Azure CLI login, managed identity or workload identity.

## HashiCorp Vault provider

`hashivault://<address>/<mount>[/<path>]` - for example `hashivault://vault.example.com:8200/secret/cratis`. Secrets are stored in the KV v2 mount at `<path>/<environment>/<key>` (the path defaults to `cratis`); the file holds that path as `ref:`. `hashivault+http://` selects plain HTTP for a development server.

Authentication is chosen from the environment:

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

Every secret is read with the current provider, stored with the new one, and the previous material is removed - the file never holds clear text in between, and moving to a vault replaces `secure:` values with `ref:` entries. Moving back to `passphrase` asks for a new passphrase.
