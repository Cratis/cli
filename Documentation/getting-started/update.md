---
title: Update Cratis CLI
description: Update Cratis CLI through Homebrew or .NET tools, replace native binaries manually, and control project template refreshes and Stage image updates.
---

`cratis update` updates Cratis CLI through the detected installation method. Homebrew installations on macOS and .NET tool installations support automatic updates; other native binaries require manual updates. Use this procedure for an existing installation, not as part of your first connection check.

## Before you update

The update command can change more than the CLI executable:

- **Homebrew on macOS:** runs `brew update`, then `brew upgrade cratis`.
- **.NET global tool:** runs `dotnet tool update -g Cratis.Cli`.
- **Project templates:** checks for and installs newer templates under `~/.cratis/templates`. Pass `--no-templates` to skip this refresh. If the template check fails, existing templates remain in place.
- **Cratis Stage:** if a Stage sandbox image already exists locally, checks for an update and can pull a newer image. It does not pull a Stage image when none is installed.

The native-binary path can still refresh templates and an existing Stage image, even though it cannot replace the CLI executable. Do not treat `cratis update` as a read-only version check.

## Run the update

1. Run the command from a terminal where the installation's package manager is available:

   ```bash title="Update to the latest version"
   cratis update
   ```

2. Read the result for the CLI, templates, and any Stage image refresh. For a successful CLI upgrade, output includes the previous and installed versions. For example:

   ```text title="Example update output"
   Updating Cratis CLI... (current: 1.2.0)
   Updated from 1.2.0 to 1.3.0
   ```

   These version numbers illustrate the output, not the current release. If the detected installed version is unchanged, the command reports `Already at the latest version (...)`. Package-manager failures are reported as update failures.

3. Run `cratis` again to use the installed version. When an upgrade succeeds, the update command shows the new release notes or offers to page through skipped releases. You can also run `cratis release-notes`; see the [release notes reference](/cli/reference/release-notes/).

## Installation-specific limits

| Installation | Update behavior |
| --- | --- |
| Homebrew on macOS | Upgrades to the latest formula; `--version` is not supported. |
| Homebrew on Linux | Prints manual Linux update instructions; run the Homebrew commands below directly. |
| .NET global tool | Updates to the latest version, or a version selected with `--version`. |
| Native binary | Prints manual update instructions; download and replace the executable yourself. |

For a Homebrew installation on Linux, `cratis update` does not upgrade the executable. Update the package directly with:

```bash title="Update Homebrew on Linux"
brew update
brew upgrade cratis
```

For a .NET global tool installation, request a specific version with:

```bash title="Install a specific version"
cratis update --version 1.2.3
```

For the latest Linux native release, repeat the [native binary installation procedure](index.mdx#install-it). A successful return from the manual-update path does not mean the executable was replaced.

## Verify and continue

Check the update report and start a new `cratis` invocation. If the package manager reports a failure, resolve that failure before retrying; do not infer an upgrade from an update-availability message alone.

If you generated AI command context with `cratis init`, run `cratis init --refresh` after upgrading to regenerate its snapshot. Return to [Getting started](index.mdx) to check a Chronicle connection, or continue with the [Chronicle command reference](/cli/chronicle/).
