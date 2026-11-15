---
title: Build slices for Cratis Studio with the Cratis CLI
description: A builder is the Cratis CLI plus a generator or coding agent of your own. The CLI talks to Studio; your program turns a build order into code, in any language.
---

A builder turns the slices of a [Cratis Studio](https://cratis.studio) event model into code. Studio decides which slices are ready, who builds them, how often a failed slice is retried and what each slice's status is. A builder only builds: it takes a slice, writes the code and says how it went.

You do not have to write a builder from scratch. **A builder is `cratis build` plus a program of your own.** The CLI does everything that involves Studio and Git; your program only turns a build order into code.

## What the CLI does for you

`cratis build run` is one loop. For every slice Studio hands it, the CLI:

1. **Connects** to Studio with the machine's builder token and tells Studio what it can build.
2. **Asks for work** and receives a build order: the slices to build, their Screenplay model and their specifications.
3. **Holds the lease.** It renews the lease on the slice every third of the lease time while the build runs. If Studio says the lease is lost, the build stops and nothing is reported.
4. **Hands the order to your program**, in a folder outside the project.
5. **Verifies** the result with the kit's verification commands and every `--verify` command, such as `dotnet test` or `npm test`.
6. **Commits** the files your program reported, on the branch Studio names.
7. **Pushes** with your own Git credentials.
8. **Reports** the outcome to Studio, with the commit and the branch.
9. **Cleans up.** Anything left uncommitted goes into a Git stash named after the order, and the repository goes back to the branch it was on.

Your program does not talk to Studio, run Git or hold a credential.

## Generators and agents

Your program is one of two kinds:

| | A generator | A coding agent |
| --- | --- | --- |
| Option | `--exec "<command>"` | `--agent <name>` |
| What it is | Any command the shell can start: a Bash script, a Node.js, Python or Go program, a .NET app, a code generator you already have | Claude Code, Codex, Gemini CLI, GitHub Copilot CLI, OpenCode, or any agent that speaks the Agent Client Protocol |
| What it gets | The build order as a JSON file | A prompt filled from the order, the Screenplay guidance, the kit's instructions and skills, and tools |
| What it gives back | An outcome as a JSON file | An outcome as a JSON file |

A generator is deterministic and needs no SDK, no Cratis package and no network access. It reads one JSON file and writes another, so it can be written in any language. See [Write a generator](write-a-generator.mdx).

A coding agent runs with its own sign-in and its permission checks on. See [Agents and build kits](agents-and-kits.md).

## Try it

```bash
cratis build connect --studio --organization acme      # once per machine; a maintainer approves it in Studio
cratis build run --exec "$HOME/slice-sheets/generate.sh" --verify "test -d slices"
```

Run the second command in a clean clone of the application's repository. [Connect and run a builder](connect-and-run.md) walks through it.

## In this section

- [Connect and run a builder](connect-and-run.md): register a machine, have it approved, and run the loop in a terminal or as a service.
- [Write a generator](write-a-generator.mdx): the contract between the CLI and your program, with the same generator in Bash, TypeScript and C#.
- [Agents and build kits](agents-and-kits.md): build with a coding agent, and describe a stack with a kit.
- [`cratis build` reference](../reference/build.md): every command and option.

## In the Studio documentation

- [Builders](https://docs.cratis.studio/developers/builders/): how builders fit into Studio, and the builder protocol for those who want to talk to Studio without the CLI.
- [Connect a builder and approve it](https://docs.cratis.studio/developers/builders/configure/connect-and-approve/): the approval, step by step, in Studio.
- [Choose who builds](https://docs.cratis.studio/developers/builders/configure/choose-builders/): pick the builder for an application or a slice.
- [Follow a build and troubleshoot](https://docs.cratis.studio/developers/builders/configure/follow-and-troubleshoot/): what Studio shows while a slice is built.
- [Build orders and outcomes](https://docs.cratis.studio/developers/builders/build/orders-and-outcomes/): every field of the order your program reads and the outcome it writes.
