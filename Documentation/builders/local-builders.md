---
title: Run the Cratis builder locally
description: Run Studio's packaged Cratis or Axon builder on your machine or in Docker, from Screenplay only through static code, code refined by AI, to AI only.
---

Studio builds slices in the cloud with its own builder. The same builder runs on your machine. It is `cratis build run` with a **level**, and it talks to Studio the way every other [builder](index.md) does, so a slice built locally shows the same status, commits and pull requests in Studio as one built in the cloud.

## The four levels

A level says how much of the application the builder produces, and how. Each one is an implementation mode Studio offers for an application in the cloud, so you can pick the same mode for a local builder.

| Level | `--level` | Studio mode | What the builder does | Calls a model |
| --- | --- | --- | --- | --- |
| Screenplay | `screenplay` | Screenplay only | Commits the Screenplay model to the `screenplay` folder of the project and writes no code. The model is the application: run it with `cratis run`. | No |
| Code | `code` | Static code | Commits the model and renders it into code with the CLI's renderer. The same model always gives the same code. | No |
| Code and AI | `code-ai` | Static code refined by AI | Renders the model deterministically, then an agent implements the behavior the model describes on top of the generated structure. Needs `--agent`. | Yes |
| AI | `ai` | AI only | Commits the model and renders nothing: an agent implements the slices from the model, following the build kit's conventions, permissions and verification commands. Needs `--agent`. | Yes |

```bash
cratis build connect --studio --organization acme       # once per machine
cratis build run --level screenplay
cratis build run --level code
cratis build run --level code-ai --agent claude --ai studio
cratis build run --level ai --agent copilot
```

Connect to the final HTTPS Studio URL: builders do not follow redirects, including during registration. For local development only, HTTP is accepted on `localhost` or a literal loopback IP such as `127.0.0.1` or `[::1]`. In a container, loopback refers to the container itself.

### What the agent is told

At `code-ai` and `ai` the agent gets the same instructions Studio's own AI modes use. The CLI spells them out at the end of the prompt, under *Instructions from the model*, and they are in the build order too:

- **Slice descriptions.** The summary of what each slice is supposed to do, from the order's `context.hints`.
- **Described handlers and queries.** The command handler descriptions and query descriptions written in the Screenplay documents.
- **Specifications.** Every specification of the slices is an acceptance criterion the agent reports a result for.
- **Feedback.** The comments people left on the slices, from `context.feedback`.

At `code-ai` the renderer has already produced the structure. The agent does not regenerate it and does not edit files with a managed header, except the places the renderer leaves open. It completes the bodies the renderer left (the Axon renderer lists them in `UNSUPPORTED.md` and `runtime/UnsupportedImplementations.java`) and adds code in new files, or files without a managed header, where the behavior needs it.

At `ai` no renderer runs. The agent writes the application from the model in the `screenplay` folder, following the kit's instructions and the patterns already in the project.

At both levels the agent does not invent behavior the model does not describe. Where the model leaves a decision open, the agent records it as an assumption in the outcome, and Studio shows it with the slice. Then it runs the verification commands, and the CLI verifies again before it commits.

To try a level without Studio, build an exported order with `cratis build once`. It writes the outcome and commits the result, and pushes only with `--push`:

```bash
cratis build once --order order.json --level code-ai --agent copilot
```

Run them in a clean clone of the application's repository. The CLI still verifies with `--verify` commands, commits on the branch Studio names, pushes with your Git credentials and reports the outcome. Choose this builder for an application, or for one slice, under **Settings** > **Building** in Studio.

### Render targets

`--target` says what the model is rendered into.

| `--target` | Result |
| --- | --- |
| `cratis` (default) | A .NET application on Cratis Arc and Chronicle, with a React frontend. Backend and user interface slices. |
| `axon` | A Java application on Axon Framework, from the Axon Stage renderer. Backend slices only. See [Axon](https://docs.cratis.studio/developers/builders/local/axon/). |

At the `ai` level the target is not rendered: it says what the agent writes.

```bash
cratis build run --level code --target axon
cratis build run --level code-ai --target axon --agent claude
cratis build run --level ai --target axon --agent copilot
```

The builder tells Studio what it can build when it connects, so Studio offers it only the parts its target produces. It names itself by target and level, such as `cratis-code-ai` or `axon-ai`.

## Run it in Docker

```bash
cratis build run --level code --docker
```

`--docker` runs the same `cratis build run` inside the packaged builder image instead of on your machine. Nothing but Docker and the CLI is needed on the host.

- The project is mounted at `/workspace`.
- The builder's connection travels in the environment: `CRATIS_STUDIO_URL`, `CRATIS_STUDIO_ORGANIZATION`, `CRATIS_BUILDER_ID` and `CRATIS_BUILDER_TOKEN`. Docker takes the token from the CLI's own environment, so it is never on a command line or in `docker ps`.
- Agent sign-ins that are set on your machine (`ANTHROPIC_API_KEY`, `CLAUDE_CODE_OAUTH_TOKEN`, `OPENAI_API_KEY`, `GEMINI_API_KEY`, `GITHUB_TOKEN`, `GH_TOKEN`) are passed on. Your `~/.gitconfig` is mounted read-only. When `GITHUB_TOKEN` or `GH_TOKEN` is set, an HTTPS push to GitHub from the container signs in with it: Git asks the container's own environment for the token when it pushes, so it is never written to a file. SSH keys and credential managers are not passed in, so use an HTTPS remote, or `--no-push` and push from your machine.
- The image follows the target: [`cratis/builder`](https://hub.docker.com/r/cratis/builder) for `cratis`, and [`cratis/studio-axon-builder`](https://hub.docker.com/r/cratis/studio-axon-builder) for `axon` (the [Axon builder](https://docs.cratis.studio/developers/builders/local/axon/)). `--image` picks another one, such as `cratis/studio-eventmodelers-builder`.

A builder host running this CLI removes `CRATIS_BUILDER_TOKEN` and `CRATIS_STUDIO_MCP_SECRET` from workload environments, including agents, generators, verification commands, Git and MCP subprocesses. Scripts must not depend on those variables. With `--docker`, the Docker launcher still forwards the selected builder token by name for compatibility. Older images do not filter their own workloads; use an image containing the updated CLI for that protection.

Custom launchers can instead set `CRATIS_BUILDER_HANDOFF=stdin-v1`, supply the URL, organization and builder ID as above, and pipe one UTF-8 token line into the inner `cratis build run`. Omit `CRATIS_BUILDER_TOKEN` in this mode. Missing input or an unknown marker fails the launch, without falling back to stored credentials. Without the marker, the host does not read stdin for credentials. The marker selects the protocol; it does not prove an image supports it.

Inside a container there is no operating system credential store, so a container reads the connection from the environment. Outside Docker the same four variables work too, for a build server that has no CLI configuration:

```bash
export CRATIS_STUDIO_URL=https://studio.example.com CRATIS_STUDIO_ORGANIZATION=acme
export CRATIS_BUILDER_ID=<id> CRATIS_BUILDER_TOKEN=<token>
cratis build run --level code
```

### Which version runs

The builder images are released with Studio, so their versions follow Studio's, not the CLI's. The CLI picks the version for you:

- If a version of the image is already on your machine, the newest one is used.
- If none is, the CLI asks Docker Hub for the newest version and pulls it, showing the progress. Without a network connection and with nothing pulled yet, the run stops and says so.
- When Docker Hub has a newer version than the one on your machine, the CLI tells you after a command, the way it does for the Stage image:

  ```text
  ↑ Builder image update available: cratis/builder 1.5.0 - run 'cratis update'
  ```

- [`cratis update`](../getting-started/update.md) pulls the newest version of every builder image already on your machine.

The check is cached and only looks at images you have pulled, and `CRATIS_NO_UPDATE_CHECK` switches it off. An image with a tag, such as `--image cratis/builder:1.4.0`, runs exactly as given.

### Use your own image

Build an image on top of the packaged builder, give it a tag and pass it with `--image`. A tag tells the CLI to run the image as it is rather than look it up on Docker Hub:

```bash
cratis build run --level code --docker --image my-builder:1.0.0
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
