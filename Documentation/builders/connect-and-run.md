---
title: Connect and run a builder with the Cratis CLI
description: Register a machine as a Cratis Studio builder, have a maintainer approve it, and run the build loop in a terminal, once offline, or as a service.
---

A builder runs on any machine that can reach Studio and the application's Git repository: your laptop, a build server or a container. This page takes one from nothing to building. Every option is in the [`cratis build` reference](../reference/build.md).

## Before you start

- The [Cratis CLI](../getting-started/index.mdx).
- A clean clone of the application's repository, with Git credentials that can push to it.
- A generator or a coding agent to build with. See [Write a generator](write-a-generator.mdx) or [Agents and build kits](agents-and-kits.md).
- A maintainer of the Studio organization, to approve the builder.

## Connect the machine

```bash
cratis build connect --studio --organization acme
```

`--studio` alone means the hosted Studio, `https://app.cratis.studio`. For another Studio, give its address, or the builder address Studio shows (`https://<studio host>/builders/connect/<organization>`), which names the organization too.

The command creates a token on this machine, registers it with the organization and prints a short code. The token is kept in the operating system's credential store and never leaves the machine except to Studio. `--name` sets what maintainers see the builder as; it defaults to the machine's name.

## Have it approved

`connect` waits until a maintainer approves the builder. In Studio, the maintainer opens **Organization settings › Builders**, finds the builder marked **Waiting for approval**, selects **Approve** and types the code the builder printed. The code is good for 15 minutes; after that, run `cratis build connect` again for a new one.

Only approve a builder you started yourself. The Studio documentation shows [the approval step by step](https://docs.cratis.studio/developers/builders/configure/connect-and-approve/).

`--no-wait` returns as soon as the machine is registered. `cratis build run` waits for the approval itself, so you can start the loop straight away.

## Run the build loop

In the clone of the repository:

```bash
cratis build run --exec "$HOME/slice-sheets/generate.sh" --verify "test -d slices"
```

or, with a coding agent:

```bash
cratis build run --agent claude --verify "dotnet test"
```

The loop connects, waits for slices, and builds each one Studio hands it. The repository must have no uncommitted changes, so a build commits only its own work. In Studio, [choose the builder](https://docs.cratis.studio/developers/builders/configure/choose-builders/) for the application or for one slice; the terminal then shows each build, its commit, and how Studio settled the slice.

| Option | What it does |
| --- | --- |
| `--verify <command>` | Runs after every build, in the project. Repeat it for several commands. A command that fails turns the outcome into `failed`. |
| `--no-push` | Commits without pushing. |
| `--once` | Stops after one slice. |
| `--project <path>` | Builds in another folder than the current one. |

`Ctrl+C` stops the loop, and Studio shows the builder as offline. Its slices wait for it.

## Build an order offline

`cratis build once` builds an order from a file, without connecting to Studio. It runs the same steps as the loop (your program, verification, commit) and writes the outcome to the standard output, or to `--outcome`. It does not push unless you add `--push`.

```bash
cratis build once --order order.json --exec "$HOME/slice-sheets/generate.sh" --verify "test -d slices" --outcome outcome.json
```

Use it to try a generator, an agent or a kit before connecting, and in CI. An example order is in Studio's [Build orders and outcomes](https://docs.cratis.studio/developers/builders/build/orders-and-outcomes/#a-build-order).

## Keep it running as a service

On macOS and Linux, install the loop as a user service from the clone of the repository. It starts when you sign in and restarts 30 seconds after it stops.

```bash
cratis build service install --exec "$HOME/slice-sheets/generate.sh" --verify "test -d slices"
cratis build service status
cratis build service uninstall
```

`install` takes the agent options of `run` (`--agent`, `--agent-command`, `--exec`, `--kit`, `--project`, `--verify`, `--no-guidance`) and `--no-push`; it does not accept `--once`, `--board`, `--modeling`, `--name` or `--unattended`. `status` says whether the service runs and where its output goes. On Windows, run `cratis build run` in a terminal instead. The [reference](../reference/build.md#service) has the service files, logs and lingering on Linux.

## Check the setup

```bash
cratis build status
```

Shows the Studio and organization, whether the builder token and Studio's MCP credential are stored, and the kit and agent of the current folder. It changes nothing.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Done. For `once`, the outcome is not `failed`. |
| `2` | Studio could not be reached. |
| `4` | Studio no longer accepts the builder's token. Register again with `cratis build connect --force`. |
| `5` | The options, the kit or the order cannot be used, the repository has uncommitted changes, or the outcome of `once` is `failed`. |

The [reference](../reference/build.md#exit-codes) lists every code.

## Related

- [Write a generator](write-a-generator.mdx): the program the loop runs.
- [Agents and build kits](agents-and-kits.md): build with a coding agent, and add a kit.
- [Follow a build and troubleshoot](https://docs.cratis.studio/developers/builders/configure/follow-and-troubleshoot/), in the Studio documentation.
