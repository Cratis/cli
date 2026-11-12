---
title: "cratis extension vendor: copy locked extension packages for air-gapped use"
description: Copy every package screenplay.lock pins, with its signature, to a local folder or a private OCI registry so restores and deployments work without reaching the public registries.
---

`cratis extension vendor` copies the packages [`screenplay.lock`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-lock) pins to a place you control,
for deployments that cannot reach the registries the references name. See
[Manage extension packages](extension.md) for references, credentials and [trust](extension.md#trust).

```bash
cratis extension vendor <DESTINATION> [--root <PATH>] [--source <FOLDER|oci://HOST/PREFIX>]
```

## What it does

Every entry of the lock is fetched, then verified against what the lock pins: the digest, the contract hash, a valid signature,
an allowed publisher. Only then is it copied. A package that is unsigned or has changed is not copied, and the command fails.
A reference in the model that the lock does not pin fails with `NotLocked`; the locked ones are still copied. Run
[`cratis extension restore`](extension-restore.md) first.

`<DESTINATION>` is one of:

| Destination | Result |
| --- | --- |
| A folder | One folder per package, named by its digest with `:` as `-`: `contract.extension` (the file the compiler reads), `package.json` (the reference, version, digest and signature) and `parts/` (the artifact: manifest, config and layers, or the npm tarball). |
| `oci://HOST/PREFIX` | The artifact and its signature pushed to your registry at `PREFIX/<repository path>`, so `ghcr.io/cratis/extension-stripe` becomes `HOST/PREFIX/cratis/extension-stripe:<version>`. The manifest is copied byte for byte, so the digest is unchanged. npm packages vendor to folders only. |

Registry credentials for the destination come from `cratis config` under the destination's host, like any other
[registry](extension.md#registry-credentials).

## Restoring from the copy

```bash
cratis extension vendor ./vendor
cratis extension restore --locked --source ./vendor
cratis extension restore --locked --source oci://registry.internal/mirror
```

A restore from the copy re-verifies the signature and the allow-list from the copy alone, and does not contact the original
registry. The digest is the lock's digest, so the copy is accepted only when it is the package the lock pinned.

To make the compiler read the packages with no restore at all, vendor into the `extensions` folder beside the lock:

```bash
cratis extension vendor ./Model/extensions
```

The compiler reads contracts from that folder before the user cache. [Commit it](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-cache-vendoring-and-restore) if the deployment builds from the repository.

## Example

```text
$ cratis extension vendor ./vendor
vendored   cratis/stripe 2.3.1 sha256:5b0c…e1
vendored   npm:@acme/charts 1.2.0 sha256:91af…07
```

## Exit codes

The [shared codes](extension.md#exit-codes) apply.

## Related

- [`cratis extension restore`](extension-restore.md) with `--source` restores from the copy.
- [`cratis extension update`](extension-update.md) moves the pins you then vendor.
- [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md) describes the lock and the compiler's cache.
