---
title: Run the Cratis builder locally
description: Build a Cratis Studio application on your own machine with the packaged Cratis builder, at the Screenplay, Code or Code and AI level, in Docker, with model calls made through Studio.
---

Studio builds slices in the cloud with its own builder. The same builder runs on your machine. It is `cratis build run` with a **level**, and it talks to Studio the way every other [builder](index.md) does, so a slice built locally shows the same status, commits and pull requests in Studio as one built in the cloud.

## The three levels

A level says how much of the application the builder produces. They are the levels Studio offers for an application in the cloud.

| Level | `--level` | What the builder does | Calls a model |
| --- | --- | --- | --- |
| Screenplay | `screenplay` | Commits the Screenplay model to the `screenplay` folder of the project and writes no code. The model is the application: run it with `cratis run`. | No |
| Code | `code` | Commits the model and renders it into code with the CLI's renderer. The same model always gives the same code. | No |
| Code and AI | `code-ai` | Renders the model, then an agent completes what a renderer leaves for judgment, such as method bodies the model only describes. Needs `--agent`. | Yes |

```bash
cratis build connect --studio --organization acme       # once per machine
cratis build run --level screenplay
cratis build run --level code
cratis build run --level code-ai --agent claude --ai studio
```

Run them in a clean clone of the application's repository. The CLI still verifies with `--verify` commands, commits on the branch Studio names, pushes with your Git credentials and reports the outcome. Choose this builder for an application, or for one slice, under **Settings** > **Building** in Studio.

### Render targets

`--target` says what the model is rendered into.

| `--target` | Result |
| --- | --- |
| `cratis` (default) | A .NET application on Cratis Arc and Chronicle, with a React frontend. Backend and user interface slices. |
| `axon` | A Java application on Axon Framework, from the Axon Stage renderer. Backend slices only. See [Axon](https://docs.cratis.studio/developers/builders/local/axon/). |

The builder tells Studio what it can build when it connects, so Studio offers it only the parts its target produces.

## Run it in Docker

```bash
cratis build run --level code --docker
```

`--docker` runs the same `cratis build run` inside the packaged builder image instead of on your machine. Nothing but Docker and the CLI is needed on the host.

- The project is mounted at `/workspace`.
- The builder's connection travels in the environment: `CRATIS_STUDIO_URL`, `CRATIS_STUDIO_ORGANIZATION`, `CRATIS_BUILDER_ID` and `CRATIS_BUILDER_TOKEN`. Docker takes the token from the CLI's own environment, so it is never on a command line or in `docker ps`.
- Agent sign-ins that are set on your machine (`ANTHROPIC_API_KEY`, `CLAUDE_CODE_OAUTH_TOKEN`, `OPENAI_API_KEY`, `GEMINI_API_KEY`, `GITHUB_TOKEN`, `GH_TOKEN`) are passed on. Your `~/.gitconfig` is mounted read-only.
- `--image` picks another image, such as the [Axon builder](https://docs.cratis.studio/developers/builders/local/axon/). The default is `ghcr.io/cratis/builder` at the version of the CLI you run.

Inside a container there is no operating system credential store, so a container reads the connection from the environment. Outside Docker the same four variables work too, for a build server that has no CLI configuration:

```bash
export CRATIS_STUDIO_URL=https://studio.example.com CRATIS_STUDIO_ORGANIZATION=acme
export CRATIS_BUILDER_ID=<id> CRATIS_BUILDER_TOKEN=<token>
cratis build run --level code
```

### Build the image yourself

The image is defined in `Docker/builder/Dockerfile` in the [CLI repository](https://github.com/Cratis/cli). It adds Git, Node.js, a JDK and Maven to the .NET SDK and installs the CLI.

```bash
docker build -f Docker/builder/Dockerfile --build-arg CLI_VERSION=<version> -t my-builder .
cratis build run --level code --docker --image my-builder
```

## Make model calls through Studio

An agent normally signs in on its own and calls its own provider. With `--ai studio`, every model call the agent makes goes to Studio instead, and Studio runs it on the AI providers the organization configured under **Settings** > **AI**.

```bash
cratis build run --level code-ai --agent claude --ai studio
```

That changes three things:

- **Usage is counted in Studio.** Tokens land in the organization's usage, per provider and model, with the calls Studio makes itself.
- **Pooling and limits apply.** The organization's provider pool, rate-limit handling and agent choices decide which model answers.
- **No model credential on the machine.** A builder on a build server or in a container needs none.

The CLI starts a small server on the loopback address of the machine, and points the agent at it with the environment variables agents read (`ANTHROPIC_BASE_URL`, `OPENAI_BASE_URL` and their keys). It speaks the OpenAI chat completions and Anthropic messages APIs, with tool calls and streaming, and forwards each call to Studio over the [builder protocol](https://docs.cratis.studio/developers/builders/protocol/ai/). The address needs a random token only the agent is given, and a call is only accepted while a slice is being built, under that slice's lease: a builder that loses its lease loses its access.

A generator started with `--exec` receives the same variables, plus `STUDIO_AI_URL` and `STUDIO_AI_TOKEN`, so a deterministic generator can ask a model for help without any model SDK or credential. See [Write a generator](write-a-generator.mdx).

`--ai studio` needs a Studio that speaks builder protocol version 2. The CLI says so and stops when the connection agreed an older version. `--ai local` is the default.

## Related

- [Connect and run a builder](connect-and-run.md)
- [Build with AI](build-with-ai.md)
- [Agents and build kits](agents-and-kits.md)
- [`cratis build` reference](../reference/build.md)
- [Local builders in the Studio documentation](https://docs.cratis.studio/developers/builders/local/)
