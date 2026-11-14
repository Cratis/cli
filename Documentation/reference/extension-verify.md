---
title: "cratis extension verify: check an extension package before it is published or loaded"
description: "Verify an extension package folder, OCI artifact or npm package before publishing: every check, its diagnostic codes, exit codes and JSON output."
---

`cratis extension verify` says whether an extension package is correct, with exact diagnostics, before it is published or
loaded. A package created by [`cratis extension new`](extension-new.md) passes without edits. See
[Manage extension packages](extension.md) for the other `cratis extension` commands.

```bash
cratis extension verify [TARGET] [--range <RANGE>] [--baseline <FOLDER>] [--strict]
    [--root <PATH>] [--source <FOLDER|oci://HOST/PREFIX>] [--format table|json]
```

`TARGET` is one of:

| Target | What is verified |
| --- | --- |
| A folder (the default is the current directory) | The folder as it ships. The contract is `contract.play` at its root, or in its `extension/` folder as in an OCI artifact. A path to `contract.play` verifies its folder. |
| A package reference | A catalog short name, an OCI reference or an npm package, as [`cratis extension restore`](extension-restore.md) resolves them, with the same registries, credentials and [trust](extension.md#trust). The package is fetched, laid out as it ships and verified. `--range` picks the release; the version [`screenplay.lock`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md#the-lock) pins is the default when it pins the reference, else the latest. |

## The checks

Every check always appears in the report with a status, so a check that did not run is never mistaken for one that passed.

| Status | Meaning |
| --- | --- |
| `passed` | The check ran and found nothing wrong. |
| `failed` | The check found at least one error. |
| `skipped` | The check applies to the package but could not run. It is never reported as a pass, and `--strict` fails it. |
| `notapplicable` | The package has nothing for the check to look at. |

| Check | What it checks | Diagnostics |
| --- | --- | --- |
| `contract` | `contract.play` compiles with no errors and no warnings. | `EXTV001` the package holds no contract; the compiler's own codes, [`PLAY0998` and `PLAY1100` to `PLAY1112`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/diagnostics.md#extension-contracts-and-the-lock), with the line. A warning is reported as an error. |
| `manifest` | A display name, a description and an icon are present. Requirements are stated. The icon and every marketplace image exist, are `png`, `jpg`, `webp` or `svg` by their content (not only their name) and are within the size limits. Captions are present. | `PLAY0999` a display name, description or icon is missing; `EXTV010` the contract states no `requires`; `EXTV011` an icon or image is not a supported format, or not the format its name says; `EXTV012` an icon is over 256 KB or an image over 2 MB; `EXTV013` an image caption is blank; `EXTV014` an image has no caption (a warning); `PLAY0998` an icon or image is not in the package or is outside it. |
| `contents` | Everything the contract declares is shipped, and nothing it ships is undeclared. | `EXTV020` the declared backend is not in the package; `EXTV021` a React or mount component is declared but the contract names no `frontend`; `EXTV022` a file is shipped that the contract does not declare; `PLAY0998` the declared frontend is not in the package. |
| `placeholders` | Every React or mount component has a placeholder, and each is static HTML: no `<script>`, no event handler attributes, no `javascript:` URLs and no external references (styles inline, images as `data:` URIs), within 256 KB. | `PLAY1102` a component has no placeholder; `PLAY1113` the placeholder is not static; `EXTV030` it is over its limit; `PLAY0998` it is not in the package. |
| `frontend` | Each component registers as its declared kind and passes the [Scene conformance kit](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md). | `EXTV060` the check could not run. See [What does not run yet](#what-does-not-run-yet). |
| `backend` | The backend starts in its tier and completes the [protocol handshake](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md) against the contract. | `EXTV070` the check could not run. See [What does not run yet](#what-does-not-run-yet). |
| `digests` | The package is the one `screenplay.lock` pins: its digest and its contract hash. Applies to a reference the lock pins. | `EXTV040` the digest differs; `EXTV041` the contract hash differs. |
| `compatibility` | The compiler can use the package as a provider. | `EXTV050` the contract requires a Screenplay version the compiler is not in; `EXTV051` with `--baseline`, the package no longer offers an ability, trigger, component, property or signal the baseline did. |

The size limits are 256 KB for an icon, 2 MB for a marketplace image and 256 KB for a placeholder.

### What ships, and `.extensionignore`

The `contents` check holds a folder to what it ships. A project folder, as [`cratis extension new`](extension-new.md) creates it,
also holds sources, specifications and scripts the build does not ship. Name them in `.extensionignore` in the package folder,
one pattern per line: `src/` ignores a folder, `Dockerfile` a file, `*.sln` a file name at the root and `**/Specs/` a folder at any depth.
Lines starting with `#` are comments. `.git/`, `node_modules/`, `bin/`, `obj/`, `.ai-work/`, `.extensionignore`, and `README*`, `LICENSE*`,
`NOTICE*` and `CHANGELOG*` are always ignored. Files in the folder of the declared `frontend` entry or `backend` assembly are shipped
with it and need no declaration. A package fetched from a registry holds exactly what it ships, so it needs none.

### What does not run yet

The `frontend` and `backend` checks have to run the package's code, which the CLI does not do yet. They are reported as `skipped` with
`EXTV060` and `EXTV070`, never as passed. Run the conformance kit in the package's own specifications, and Stage performs the
handshake when it loads the package. `--strict` fails a package until these checks can run.

## Output and exit codes

The report is human-readable by default and machine-readable with `--format json`: the target, whether it passed and, for each check,
its status and diagnostics (`check`, `code`, `severity`, `message`, `path` and `line`).

| Code | Meaning |
| --- | --- |
| `0` | The package is correct: no check failed. With `--strict`, also no warning and no check that could not run. |
| `5` | A check failed. |
| `1` | The target could not be found, or the reference could not be resolved. |
| `2` | A registry could not be reached. |
| `4` | A registry refused the credentials. |

Other failures to fetch a package, such as a signature or publisher the [trust](extension.md#trust) rules refuse, are `5`.

```bash
cratis extension verify ./Stripe
cratis extension verify ghcr.io/acme/extension-stripe --range "^2"
cratis extension verify ./Stripe --baseline ./previous-release --strict --format json
```

## Related

- [Create an extension package](extension-new.md): what to start from.
- [Extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md): the contract, the manifest and the lock.
- [Extension packages in Stage](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md): admission, trust and asset serving.
- [Templates README](https://github.com/Cratis/Templates/blob/main/README.md): the extension templates.
