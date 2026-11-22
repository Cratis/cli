---
title: Build with a coding agent and a build kit
description: Run a Cratis Studio builder with Claude Code, Codex, Gemini CLI, Copilot CLI, OpenCode or any ACP agent, and describe a stack with a build kit.
---

A builder can hand each build order to a coding agent instead of a [generator](write-a-generator.mdx). The CLI still does the rest: it talks to Studio, verifies, commits, pushes and reports. A build kit describes the stack the builder builds, for agents and generators alike. The [`cratis build` reference](../reference/build.md) has every option.

This page is the reference for agents and kits. For a complete, working kit to start from, see [Build with AI](build-with-ai.md), where the agent writes the code, and [Deterministic with AI on top](deterministic-and-ai.md), where a generator in the kit writes the structure and the agent completes it.

## Choose an agent

```bash
cratis build run --agent claude --verify "dotnet test"
```

| `--agent` | Agent | How it runs |
| --- | --- | --- |
| `claude` | Claude Code | Through its own command line. |
| `codex` | Codex | Over the Agent Client Protocol, through `npx -y @zed-industries/codex-acp`. |
| `gemini` | Gemini CLI | Over the Agent Client Protocol, through `gemini --experimental-acp`. |
| `copilot` | GitHub Copilot CLI | Over the Agent Client Protocol, through `copilot --acp`. |
| `opencode` | OpenCode | Over the Agent Client Protocol, through `opencode acp`. |
| `cursor`, `acp` | Any ACP agent | Over the Agent Client Protocol, through the command you give with `--agent-command`. |

`--agent-command` overrides the command for any agent. The agent runs with its own sign-in. `cratis build init --agent <agent>` records the agent in the project, so `run` and `once` use it without `--agent`.

## What an agent gets

- **A prompt** filled from the order, telling it which slices to build and where to write its outcome. The order itself is in a folder outside the project.
- **The Screenplay session guidance**, the version Studio pins, unless you add `--no-guidance`.
- **The kit's instructions and skills**, when the project has a kit.
- **Tools**: Screenplay's tools on the order's model, and Studio's board tools when the organization's MCP credential is configured.

The agent writes the same [outcome](write-a-generator.mdx#the-outcome) a generator does. If it stops without one, the CLI asks it to go on, up to twice.

An agent is never run with its permission checks switched off. It may read the project and the folders handed to it, write only the paths the kit permits, and run only the commands the kit permits and the verification commands. Web fetches are refused. No credential is written into a prompt, a file the agent reads, an outcome or the log.

### Build progress

For build contract 2 orders, the CLI reports preparation, building, verification and commit activity to Studio. ACP agents also supply tool-call titles and status changes while running. Message chunks, thoughts and raw tool arguments or results are not used as progress. Other agents still receive the host's lifecycle steps; generators can [append steps to a progress file](write-a-generator.mdx#optional-progress).

Progress is redacted and best effort: a bounded queue may drop updates with a local diagnostic, and connection errors do not turn a successful build into a failure. Progress does not renew the lease; heartbeats continue independently. When Studio reports that the lease is gone, the CLI cancels the build and sends no outcome. Contract 1 orders behave as before, without progress requests.

### Give agents Studio's tools

```bash
export CRATIS_STUDIO_MCP_SECRET=...   # the organization's MCP client secret
cratis build connect --studio --organization acme --mcp-client-id <client id>
```

The client id and secret come from **Organization settings › MCP** in Studio. The secret is read from `CRATIS_STUDIO_MCP_SECRET`, or asked for in the terminal; it is never taken on the command line. The same credential lets `cratis studio fetch` export the context of a slice, feature, event model or domain.

## Build kits

Without a kit, a builder builds the backend of every slice type, may write anywhere in the project, and verifies with `--verify` only. A kit narrows and describes that:

| A kit says | So that |
| --- | --- |
| `sliceTypes` | Studio offers the builder only those slice types. |
| `ui: true` | Studio offers user interface orders too, not only backend ones. |
| `verify` | Every build runs these commands before committing. |
| `permissions` | The paths agents may write and the CLI commits, and the commands agents may run. |
| `git` | The branch and commit message when Studio does not name them. |
| `instructions`, `skills`, `prompt` | What an agent is told. |
| `scaffold`, `requires`, `existingCodebase` | How `init` sets up a new or existing project. |

### Build contract versions

The host supports build contracts `"1"` and `"2"`. A kit without `contract.versions` still accepts only `"1"`; the CLI offers only versions supported by both host and kit. A compatible kit can opt into progress with:

```yaml
contract:
  versions: ["1", "2"]
```

Studio chooses the version for each order. Build-contract versions are separate from connection-protocol versions: contract 2 progress also works over connection protocol 1.

### Install a kit

```bash
cratis build init --kit cratis-csharp --agent claude
```

`--kit` is a kit Studio registers by name (`cratis-csharp`, `node`), a Git URL (end it with `#<branch, tag or commit>` to pin it), or a folder. `init` validates the kit, checks the commands it requires, scaffolds an empty project, copies the kit to `.cratis/build/kit` and installs its instructions and skills where the agent reads them. It records what it did in `.cratis/build/kit.json`; `run`, `once` and `service install` read the kit from there. `--kit <path>` on `run` or `once` uses another kit for one run.

### Write a kit

A kit is a folder with a `build-kit.yaml` (or `build-kit.json`) manifest and the files it names:

```yaml title="build-kit.yaml"
kitVersion: "1"
name: slice-sheets
version: 1.0.0
description: Writes a Markdown sheet for each slice.
sliceTypes: [stateChange, stateView, automation, translation]
ui: false
instructions: ./AGENTS.md
prompt: ./build-slice.md
verify:
  - test -f slices/{feature}/{slice}.md
git:
  branch: "build/{feature}"
  commit: "Build {slices} ({part})"
permissions:
  paths: ["slices/**"]
  commands: [ls]
```

`kitVersion`, `name`, `version`, `description`, `sliceTypes`, `instructions`, `prompt` and `verify` are required. Paths inside the kit start with `./`. Commands, prompts, branch names and commit messages may use these placeholders: `{order}`, `{outcome}`, `{slice}`, `{slices}`, `{sliceId}`, `{feature}`, `{featurePath}`, `{application}`, `{part}`, `{attempt}` and `{conventions}`. `{slice}` and `{sliceId}` are the order's first slice; `{slices}` names them all. In a command, each value is quoted for the shell, so do not quote placeholders yourself.

Check a kit the way Studio and the builder do:

```bash
cratis build kit validate ./slice-sheets-kit
```

It reports each problem with a JSON pointer into the manifest. The Studio documentation describes [the kit format](https://docs.cratis.studio/developers/builders/configure/cratis-build/) in full.

## Related

- [Connect and run a builder](connect-and-run.md)
- [Build with AI](build-with-ai.md)
- [Deterministic with AI on top](deterministic-and-ai.md)
- [Write a generator](write-a-generator.mdx)
- [`cratis build` reference](../reference/build.md)
