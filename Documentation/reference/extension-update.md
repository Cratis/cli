---
title: "cratis extension update: move extension references to newer releases"
description: Update one system's extension or every referenced extension to the newest release within its range, see how each contract changes and which uses in the model it breaks, before screenplay.lock is written.
---

`cratis extension update` moves a pin forward. It resolves each reference to the newest release in its range (the newest
release when the model line names no version), shows how the contract changes, and checks every use in the model against the
new contract before it writes [`screenplay.lock`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-lock). See
[Manage extension packages](extension.md) for references, credentials and [trust](extension.md#trust).

```bash
cratis extension update [SYSTEM] [--dry-run] [--allow-incompatible] [--root <PATH>] [--source <FOLDER|oci://HOST/PREFIX>]
```

## What it does

| Argument or option | Meaning |
| --- | --- |
| `SYSTEM` | The system whose extension to update. Every referenced extension when omitted. A system that names no extension fails `Unresolvable`. |
| `--dry-run` | Report what would change; write nothing. |
| `--allow-incompatible` | Write the lock even when a new contract no longer offers something the model uses. |
| `--source` | Resolve from a vendored folder or a private mirror instead of the registry. |

For each reference the newest release that satisfies its range is resolved, and checked for a valid signature, an allowed
publisher, and the publisher the lock already records. Then:

- **Nothing newer.** The reference is `unchanged`.
- **Outcomes.** A reference ends as `updated`, `would-update` (with `--dry-run`), `unchanged` or `failed`.
- **A newer release.** The report lists, against the contract the lock pins now:
  - what the new contract adds (`+`): abilities, triggers and components that are new;
  - what it no longer offers or offers differently (`-`): an ability, trigger or component that is gone, or whose typed inputs, returns, payload, properties or raised signals changed;
  - every use in the model the new contract does not offer (`!`), with the compiler's diagnostic, for example a reaction that waits for a trigger the new contract no longer raises.
- **No incompatible use.** The lock entry is moved: version, digest, contract hash and publisher.
- **An incompatible use.** The lock is **not** changed for that reference unless you pass `--allow-incompatible`. The command
  exits with `5`.

How a member compares is decided by what it offers, the same rule the compiler uses when one extension replaces another
([provider compatibility](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#provider-compatibility)): names and typed shapes, not where a member sits in the
contract file, so a reordered contract is not a change. A reference that is not locked yet is pinned, with nothing to compare.

## Example

```text
$ cratis extension update --dry-run
would-update cratis/stripe 2.4.0 sha256:5b0c…e1
cratis/stripe: 2.3.1 -> 2.4.0
  + ability 'RefundPayment' is new
  ! PLAY1112: Extension 'cratis/stripe' does not offer trigger 'PaymentSucceeded' used as 'Payments.PaymentSucceeded'.
```

With `--dry-run` the lock is untouched; the report still says what would happen.

## Exit codes

The [shared codes](extension.md#exit-codes) apply. `5` also covers an incompatible update that was not written.

## Related

- [`cratis extension restore`](extension-restore.md) fills the cache from the lock.
- [`cratis extension vendor`](extension-vendor.md) copies what the lock now pins.
- [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md) lists what a contract offers.
