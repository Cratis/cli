---
title: Manage Screenplay extension packages with Cratis CLI
description: Restore, update and vendor the extension packages a Screenplay model references, with the lock, the project allow-list, registry credentials and exit codes that apply to every cratis extension command.
---

A [Screenplay](screenplay.md) model references an extension package with `extension "<reference>" [version <range>]`
on a system. The compiler never fetches a package: it reads the contract of each one from `screenplay.lock` and a
local, digest-keyed cache. The `cratis extension` commands create and verify packages and resolve a reference, move a pin and copy packages
for offline use.

| Command | What it does |
| --- | --- |
| [`cratis extension new`](extension-new.md) | Creates a full extension or a frontend-only component package that already builds and verifies. |
| [`cratis extension verify`](extension-verify.md) | Checks a package folder or a published package: contract, manifest, contents, placeholders, digests and compatibility. |
| [`cratis extension restore`](extension-restore.md) | Fills the cache from the lock. Pins a reference the lock does not have yet. |
| [`cratis extension update`](extension-update.md) | Moves references to the newest release in their range and shows how each contract changes. |
| [`cratis extension vendor`](extension-vendor.md) | Copies every locked package to a folder or a private registry. |

Read [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md) first for what a package contains, the
[lock format](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-lock) and the diagnostics the compiler reports
([`PLAY1107`–`PLAY1111`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/diagnostics.md#extension-contracts-and-the-lock)). The
[Screenplay tool](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#restore-extensions) restores from a directory without any network; the CLI adds the registries.

## Options every extension command shares

| Option | Meaning |
| --- | --- |
| `--root <PATH>` | The model root holding the `.play` files and `screenplay.lock`. Defaults to the current directory, honoring `mcpServers.screenplay.root` in `.cratis/ai.json`. |
| `--source <FOLDER\|oci://HOST/PREFIX>` | Fetch from a vendored folder or from a private registry that mirrors the packages, instead of from the registries the references name. |
| `-e <NAME>`, `--environment <NAME>` | The [environment](../environments/index.md) whose `cratis config` holds registry credentials. |
| `-o <FORMAT>` | The [output format](output-formats.md): readable lines in a terminal, JSON when output is piped. The JSON lists one outcome per reference and, for `update`, the contract changes. |

## Where packages come from

A reference is one of three things. The catalog is a naming convention, not a service.

| Reference | Resolves to |
| --- | --- |
| `"cratis/stripe"` | `ghcr.io/cratis/extension-stripe`, an OCI artifact. |
| `"ghcr.io/acme/extension-ledger"` | That OCI artifact. |
| `"npm:@acme/charts"` | An npm package, for frontend-only packages. |

An OCI package is the artifact at `host/repository:<version>`. Its contract is the layer titled `contract.play`, or the file
`extension/contract.play` inside a tar layer. An npm package keeps `contract.play` at its root. The package **digest** is the OCI
manifest digest, or the SHA-256 of the npm tarball; the lock pins it together with the hash of the contract.

## Registry credentials

Public packages need nothing. For a private registry, store the credential in the selected environment with
`cratis config`; it is never read from the model or the lock, and no command prints it:

```bash
echo -n "$REGISTRY_TOKEN" | cratis config set extensions.registry.registry.internal.token --secret
cratis config set extensions.registry.registry.internal.username ci
cratis config set extensions.registry.registry.internal.password --secret   # prompts, hidden
```

The key is `extensions.registry.<host>.<field>`, where `<host>` is the registry host with its port when it is not the
default, and the field is `token`, or `username` with `password`. A registry is asked anonymously first; the credential is
read, and a passphrase asked for, only when the registry challenges. An npm registry other than `registry.npmjs.org` is set
with `extensions.npm.registry`. Error messages never contain a credential value.

See [Secrets](../environments/secrets.md) for secrets providers and how CI supplies the passphrase.

## Trust

Naming a package in a model is not trusting it. Before a package is pinned, restored or vendored, the CLI checks that it
is signed, that the signature is valid for exactly that package digest, and that the project allows its publisher.

**Signatures.** An OCI package carries a cosign-layout signature artifact tagged `sha256-<manifest digest>.sig`. Its payload
names the manifest digest, the `publisher` identity and the public key; the signature is ECDSA P-256 over SHA-256. An npm
package is signed by its registry over `name@version:integrity` with a key the registry publishes at `/-/npm/v1/keys`, and its
publisher is its scope (`npm:@acme`) or its name. The publisher the lock records is the one the signature names.

**The allow-list.** `.cratis/extensions.json`, committed with the project, lists the publishers it trusts:

```json
{
  "publishers": [
    {
      "publisher": "https://github.com/cratis",
      "key": "sha256:<fingerprint of the publisher's key>",
      "references": ["ghcr.io/cratis/*"]
    },
    { "publisher": "npm:@acme" }
  ]
}
```

`key` is required for OCI packages, which carry their own key, and is ignored for npm packages, whose key the registry
vouches for. `references` limits what the publisher may supply; `*` matches any run of characters, and an entry without it
allows every reference.

| Situation | Result |
| --- | --- |
| The package is not signed | Fails `NotSigned`. Nothing to trust. |
| The signature is invalid, signs another package, or the key is not the one the allow-list records | Fails `SignatureInvalid`. A changed key is never offered for trust; review it and edit the allow-list. |
| The publisher is not on the allow-list and the terminal is interactive | Asks once, showing the publisher and the key fingerprint. Yes records the entry in `.cratis/extensions.json`. |
| The publisher is not on the allow-list and the run cannot ask (CI, an AI agent, or output that is not a terminal) | Fails `Unallowed`. |
| The publisher is allowed but not for this reference | Fails `Unallowed`. |
| The lock records another publisher than the one that signed | Fails `Mismatched`. |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Every reference succeeded. |
| `1` | A reference or a pinned release could not be found. |
| `2` | A registry could not be reached. |
| `4` | A registry refused the credentials, or asks for one that is not configured. |
| `5` | A reference is not locked (`--locked`), or a package is unallowed, unsigned, mismatched, or incompatible with the model. |

With several failures the most serious code wins: `4`, then `2`, then `5`; `1` only when every failure is a missing package.
The JSON output carries the kind of each failure (`Unresolvable`, `NotSigned`, `SignatureInvalid`, `Unallowed`,
`Mismatched`, `NotLocked`, `Incompatible`, `Unreachable`, `Unauthorized`).

## One implementation

The three commands share one implementation: a package is resolved, fetched, verified and checked against the allow-list the
same way whichever command asks for it. They share the options above, and related commands for extensions join this group.
