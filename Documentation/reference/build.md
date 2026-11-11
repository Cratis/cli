---
title: Build slices from Cratis Studio with any coding agent
description: Connect a machine to Cratis Studio as a builder and build slices with Claude Code, an agent over the Agent Client Protocol, or a generator.
---

`cratis build` turns a machine into a builder for [Cratis Studio](https://cratis.studio). Studio offers the slices of an event model that are ready to build; the builder takes one, hands it to a coding agent or a generator, verifies the result with the kit's commands, commits it and reports the outcome. Studio decides what is offered, retries and each slice's status; the builder only builds.

```bash
cratis build connect --studio https://cratis.studio --organization acme
cratis build init --kit cratis-csharp --agent claude
cratis build run
```

| Command | What it does |
|---|---|
| [`cratis build connect`](#connect) | Registers this machine as a builder and waits for a maintainer to approve it. |
| [`cratis build init`](#init) | Installs a build kit into a project. |
| [`cratis build run`](#run) | Runs the build loop, or with `--modeling` connects an agent to a board. |
| [`cratis build once`](#once) | Builds an exported order offline. |
| [`cratis build status`](#status) | Shows the connection and the project's kit and agent. |
| [`cratis build kit validate`](#kit-validate) | Validates a kit, for kit authors. |
| [`cratis studio fetch`](#studio-fetch) | Fetches the context of a slice, feature, event model or domain for an agent. |

## Agents and generators

| `--agent` | Agent | How it runs |
|---|---|---|
| `claude` | Claude Code | Natively, through its own command line. Nothing else to install. |
| `codex` | Codex | Over the Agent Client Protocol, through `npx -y @zed-industries/codex-acp`. |
| `gemini` | Gemini CLI | Over the Agent Client Protocol, through `gemini --experimental-acp`. |
| `copilot` | GitHub Copilot CLI | Over the Agent Client Protocol, through `copilot --acp`. |
| `opencode` | OpenCode | Over the Agent Client Protocol, through `opencode acp`. |
| `cursor`, `acp` | Any ACP agent | Over the Agent Client Protocol, through the command given with `--agent-command`. |

`--agent-command` overrides the command for any agent. The agent runs with its own sign-in.

A deterministic generator needs no agent: `--exec <command>` runs the command in the project with the order in the file named by `STUDIO_BUILD_ORDER`. The command writes its outcome to the file named by `STUDIO_BUILD_OUTCOME`; the filled prompt is in the file named by `STUDIO_BUILD_PROMPT` for a generator that wants it.

## What an agent gets

Every session gets, without anything installed in the project:

- **The order**, written to a folder outside the project, with the prompt filled from it.
- **The Screenplay session guidance**, the version Studio pins, verified against its checksum and cached under `~/.cratis/cache/screenplay-session`. Claude Code loads it as a plugin and a system-prompt addition; agents over ACP read its `AGENTS.md`. `--no-guidance` leaves it out.
- **The kit's instructions and skills.**
- **Tools**: Screenplay's tools on the order's model, served by this CLI, and Studio's board tools when the organization's MCP credential is configured.

An agent is never run with its permission checks switched off. It may read the project and the folders handed to it, write only the paths the kit permits, and run only the commands the kit permits and its verification commands. Claude Code is started so that it refuses everything else; an agent over ACP asks, and the builder answers from the same rules. Web fetches are refused.

No credential is written into a prompt, a file the agent reads, an outcome or the log. Studio's MCP token reaches Claude Code through an environment variable its MCP configuration reads, and an ACP agent through the session setup. What the builder logs and reports is scrubbed of every credential it holds and of anything that looks like one.

## Verification, commits and outcomes

The agent writes an outcome (`built`, `blocked` or `nothingToDo`); the builder keeps only what Studio's build contract declares for that kind. Then:

1. Every verification command of the kit, and each `--verify`, runs in order in the project. If any exits with anything but 0, the outcome is `failed` and retryable, whatever the agent wrote.
2. The builder commits the files the agent reported that the kit permits, on the branch Studio names for a Studio-managed build, or on the kit's `git.branch` otherwise, with Studio's or the kit's message. Files outside the permitted paths are never committed.
3. It pushes with your own Git credentials (not with `--no-push`) and reports the commit and branch with the outcome.

Anything left uncommitted after a build is put away in a Git stash named after the order, never discarded, and the repository goes back to the branch it was on.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Done. For `once`, the outcome is not `failed`. |
| `2` | Studio could not be reached. |
| `3` | Studio refused the registration. |
| `4` | Studio no longer accepts the builder's credential; register again with `connect --force`. |
| `5` | The options, the kit or the order cannot be used, or the outcome of `once` is `failed`. |

## connect

```bash
cratis build connect --studio <URL> --organization <NAME> [--name <NAME>] [--mcp-client-id <ID>] [--mcp-url <URL>] [--force] [--no-wait] [--insecure-file-store]
```

Creates a token on this machine, registers with the organization and prints a short code. A maintainer types the code in Studio under **Settings › Builders** to approve the builder; `connect` waits until they do. The token is kept in the operating system's credential store (`--insecure-file-store` keeps it in a `0600` file instead) and never leaves this machine except to Studio.

`--mcp-client-id` configures the organization's MCP credential from **Settings › MCP**, so agents get Studio's tools and `cratis studio fetch` works. The secret is read from `CRATIS_STUDIO_MCP_SECRET`, or asked for in a terminal; it is never taken on the command line.

## init

```bash
cratis build init --kit <KIT> [--kit-folder <PATH>] [--agent <AGENT>] [--project <PATH>] [--existing] [--studio <URL>]
```

`--kit` is a kit Studio registers by name (`cratis-csharp`, `node`), a Git URL, or a folder. A Git URL may end in `#<branch, tag or commit>` to pin the kit; `--kit-folder` names the folder inside the repository that holds `build-kit.yaml`. The kit is validated before anything is copied; a registered kit's archive is verified against its checksum. Then `init`:

- checks the commands the kit requires,
- scaffolds an empty project from the kit and runs its scaffold commands; a project that is not empty (or `--existing`) is left as it is, which needs a kit that learns existing codebases: the first build learns the project's conventions into the kit's conventions file and later builds reuse them,
- copies the kit to `.cratis/build/kit`, and
- installs the kit's instructions and skills where the agent reads them: `CLAUDE.md` and `.claude/skills` for Claude Code, `GEMINI.md` and `.agents/skills` for Gemini CLI, `AGENTS.md` and `.agents/skills` for the others. The instructions go between `<!-- cratis-build-kit:<name> -->` and `<!-- /cratis-build-kit:<name> -->`, followed by a line per skill; the rest of an existing instruction file is kept, and running `init` again replaces only that block.

The installation is recorded in `.cratis/build/kit.json`: the kit's name and version, the archive's checksum (or the Git URL and the commit it resolved to), the agent, and every path `init` wrote. `run` and `once` read the kit and agent from it.

## run

```bash
cratis build run [--agent <AGENT> | --exec <COMMAND>] [--agent-command <COMMAND>] [--kit <PATH>] [--project <PATH>] [--verify <COMMAND>]... [--once] [--no-push] [--no-guidance]
```

Connects, asks Studio for work and builds each slice it is given, renewing the slice's lease while it builds. When Studio says the lease is lost, the build stops and nothing is reported. The project must be a Git repository without uncommitted changes. `--once` stops after one slice; `Ctrl+C` stops the loop, and the builder shows as offline.

### Modeling on a board

```bash
cratis build run --modeling --agent <AGENT> --board <BOARD> [--name <NAME>] [--unattended]
```

Connects the agent to an event model board instead of building. `--board` is the board's identifier or its address in Studio. It shows on the board as connected, under `--name` or the agent's product name (`Claude Code`, `Codex`, `Gemini CLI`, `Cursor`, `Copilot CLI`, `OpenCode`), and people hand it work by mentioning it in the board chat; its replies appear in the chat. By default the agent proposes and a person confirms: it may only read the board until a person confirms its proposal, and a declined proposal changes nothing. `--unattended` lets it change the board at once. Everything the agent changes on the board is attributed to it. The command ends when Studio ends the session; a refused connection ends it with the reason and a non-zero exit code. It needs the organization's MCP credential (`connect --mcp-client-id`).

## once

```bash
cratis build once --order <FILE> [--agent <AGENT> | --exec <COMMAND>] [--outcome <FILE>] [--push] [...]
```

Builds an order exported from Studio without a connection, and writes the outcome to the standard output or `--outcome`. Useful for trying a kit or an agent, and in CI.

## status

```bash
cratis build status
```

Shows the Studio and organization, whether the builder token and Studio's MCP credential are stored, and the kit and agent of the current folder. Changes nothing.

## service

Keeps a builder running without a terminal: the build loop of one project runs as a user service that starts when you sign in and restarts 30 seconds after it stops.

```bash
cratis build service install --agent claude
cratis build service status
cratis build service uninstall
```

`install` takes the options of `run` (`--agent`, `--agent-command`, `--exec`, `--kit`, `--project`, `--verify`, `--no-guidance`, `--no-push`) and needs a machine registered with `connect` and a project with a kit. Installing again replaces the service. Each project gets its own service, named `cratis-build-<folder>-<hash>`.

| Operating system | Service manager | Service file | Output |
| --- | --- | --- | --- |
| macOS | launchd agent in your sign-in session | `~/Library/LaunchAgents/io.cratis.<name>.plist` | `~/Library/Logs/Cratis/<name>.log` |
| Linux | systemd user unit | `~/.config/systemd/user/<name>.service` (or under `$XDG_CONFIG_HOME`) | `journalctl --user -u <name>.service` |

The service gets the `PATH` of the shell you installed it from, so it finds the agent, Git and the kit's tools, and nothing else from your environment. The builder token stays in the credential store; on Linux the service reads it from the Secret Service of your session. A systemd user service stops when you sign out unless lingering is on for your user (`loginctl enable-linger`). Windows has no service manager the builder supports; run `cratis build run` there.

`status` reports whether the service is installed and running and where its output goes, and changes nothing. `uninstall` stops the service and removes its file.

## kit validate

```bash
cratis build kit validate [PATH]
```

Validates a kit the way Studio and the builder do: the manifest (`build-kit.yaml` or `build-kit.json`) against the format, that every file and folder it names is in the kit and not empty, that every skill has a `SKILL.md`, and that every placeholder is one a builder fills. Each problem is reported with a JSON pointer into the manifest. Studio's builder documentation describes the format.

## studio fetch

```bash
cratis studio fetch --context <slice|feature|eventModel|domain> --id <ID> [--format markdown|text|json|yaml|toon] [--out <PATH>] [--no-guidance]
```

Fetches the context of a slice, a feature, an event model or a domain through Studio's export: its commands, events, read models, specifications and Screenplay, with the Screenplay guidance unless `--no-guidance`. `text` is Markdown; TOON fits a large context into a token budget. Writes to the standard output, or to `--out`. Uses the organization's MCP credential configured with `cratis build connect --mcp-client-id`.
