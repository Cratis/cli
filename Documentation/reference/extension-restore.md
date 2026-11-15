---
title: "cratis extension restore: fill the extension cache from screenplay.lock"
description: Restore extension packages from screenplay.lock into the local cache so a Screenplay model compiles, pin new references, and fail CI with --locked.
---

`cratis extension restore` makes a model compile: it fills the digest-keyed cache the compiler reads with the contract of every
package `screenplay.lock` pins. It is the command behind `PLAY1108` ("the contract is not restored") and, without `--locked`,
`PLAY1107` ("a reference is not in the lock"). Read [Manage extension packages](extension.md) for references, credentials and
[trust](extension.md#trust), and the [lock reference](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-lock) for the file itself.

```bash
cratis extension restore [--locked] [--root <PATH>] [--source <FOLDER|oci://HOST/PREFIX>]
```

## What it does

For every `extension` line in the model's systems:

1. **The lock pins it for the same range.** The cache is checked by the pinned digest and contract hash. A hit is reported as
   `cached`. A miss fetches exactly the pinned release: first from the `extensions` folder beside the lock, then from
   `--source`, then from the registry. The package must still have the pinned digest and contract hash, and must pass the
   [trust checks](extension.md#trust). It is then stored in the cache and reported as `restored`. A range is never looked at.
2. **The lock does not pin it, or pins it for another range.** The reference is resolved to the newest release in its range
   (the newest release when the line names no version), checked, stored, and a lock entry is written with the version, digest,
   contract hash and publisher. It is reported as `pinned`.

The lock is written when entries changed, and only then, and the file keeps its canonical order so a diff shows real changes.
Lock entries no model line refers to are left alone. References that fail do not stop the others: every reference gets an outcome,
the lock keeps the entries that succeeded, and the exit code reports the failure.

## Restoring in CI

```bash
cratis extension restore --locked
```

`--locked` never resolves a range and never changes `screenplay.lock`, not even its formatting. A reference the lock does not
pin, or pins for another range, fails with `NotLocked` (exit `5`); a pinned package that has changed fails with `Mismatched`.
The cache is still filled for pinned references, so the compiler step that follows finds every contract.

## Options

| Option | Meaning |
| --- | --- |
| `--locked` | Fail instead of resolving a reference the lock does not pin; never write the lock. |
| `--root <PATH>` | The model root. See [shared options](extension.md#options-every-extension-command-shares). |
| `--source <FOLDER\|oci://HOST/PREFIX>` | Fetch from a [vendored](extension-vendor.md) folder or a private mirror. |

## Example

```text
$ cratis extension restore
pinned     cratis/stripe 2.4.0 sha256:5b0c…e1
restored   npm:@acme/charts 1.2.0 sha256:91af…07
Wrote screenplay.lock.
```

```text
$ cratis extension restore --locked
failed     cratis/ledger NotLocked: 'cratis/ledger' is not pinned for version 'latest' in screenplay.lock; run 'cratis extension restore' and commit the lock.
```

## Outcomes and exit codes

Each reference ends as `cached`, `restored`, `pinned` or `failed`. A failure names its kind (`Unresolvable`, `NotSigned`,
`SignatureInvalid`, `Unallowed`, `Mismatched`, `NotLocked`, `Unreachable`, `Unauthorized`) and the [exit codes](extension.md#exit-codes)
are shared by every extension command.

## Related

- [`cratis extension update`](extension-update.md) moves a pin to a newer release.
- [`cratis extension vendor`](extension-vendor.md) copies the locked packages for offline restores.
- [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md) and the [Screenplay tool's restore](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#restore-extensions).
