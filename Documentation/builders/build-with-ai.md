---
title: Build slices with a coding agent and the Cratis CLI
description: Let a coding agent build Cratis Studio slices with cratis build, inside the files, commands and checks of a build kit, with a complete sample kit.
---

A coding agent can build a slice from its build order: read the specifications, write the code and the tests, and run them until they pass. `cratis build` starts the agent, keeps it to the files and commands the kit permits, and runs the kit's checks itself before anything is committed. You describe your stack once, in a [build kit](agents-and-kits.md#build-kits), and the agent follows it on every slice.

Choose this way when you want the most code for the least effort, on any stack. The result depends on two things you control: the instructions the agent gets, and the checks that catch what it gets wrong. The Studio documentation has the long walkthrough: [Build with AI](https://docs.cratis.studio/developers/builders/build/with-ai/).

## Quick start with a registered kit

```bash
cratis build init --kit node --agent claude              # in an empty Git repository for the application
git add . && git commit -m "Install the node kit"        # a build starts from a clean project
cratis build connect --studio --organization acme        # once per machine
cratis build run
```

`init` checks that the kit's tools are installed, scaffolds the project, copies the kit to `.cratis/build/kit`, and writes the kit's instructions where the agent reads them: `CLAUDE.md` for Claude Code, `GEMINI.md` for Gemini CLI, `AGENTS.md` for the others. `run` uses the kit and the agent `init` recorded. [Agents and build kits](agents-and-kits.md) lists the agents and what each one gets.

## What the agent may do

- It may **read** the project and the order's folder.
- It may **write** only the files that match the kit's `permissions.paths`, and its outcome file. It never writes inside `.git`.
- It may **run** only the commands that start with an entry of `permissions.commands` or a `verify` command, followed by a space or nothing. A command that chains, pipes, redirects or substitutes another (`&&`, `;`, `|`, `>`, `` ` ``, `$(`) is refused, whatever it starts with.
- It never commits, pushes or switches branches. The CLI does that after the `verify` commands pass.

Permissions are not a sandbox: a permitted command runs with your user's rights. Permit only commands you would run yourself, such as your compiler and your test runner.

## Sample: a kit of your own

This kit builds slices in plain TypeScript on Node.js, tested with Vitest. Make a folder `ts-vitest` with these files:

```text
ts-vitest/
├── build-kit.yaml         # the manifest
├── AGENTS.md              # the stack instructions
├── build-slice.md         # the prompt for one build order
└── scaffold/              # the files of a new project
    ├── .gitignore
    ├── package.json
    └── tsconfig.json
```

The manifest builds state changes and state views, scaffolds a new project and runs `npm install`, verifies with the type check and the feature's tests, lets the agent write only under `src`, and permits only the type check and the tests:

```yaml title="ts-vitest/build-kit.yaml"
kitVersion: "1"
name: ts-vitest
version: 1.0.0
description: Plain TypeScript slices on Node.js, specified with Vitest.
sliceTypes: [stateChange, stateView]
scaffold:
  path: ./scaffold
  commands:
    - npm install
instructions: ./AGENTS.md
prompt: ./build-slice.md
verify:
  - npx tsc --noEmit
  - npx vitest run src/{feature}
git:
  commit: "Build {slices}"
permissions:
  paths: ["src/**"]
  commands:
    - npx tsc --noEmit
    - npx vitest run
requires:
  - command: node
    version: ">=22"
  - command: npm
```

The instructions say how code is written in this stack. What to build comes from the order:

```md title="ts-vitest/AGENTS.md"
# TypeScript slices

This project is a set of event-sourced slices in plain TypeScript on Node.js, with no framework.

- Each feature is a folder under `src/`, named exactly like the feature: `src/Invoicing/`.
- Each slice is one file named after the slice, `src/Invoicing/PayInvoice.ts`, with its tests beside it in
  `src/Invoicing/PayInvoice.test.ts`.
- Events and commands are TypeScript types. An event has a `type` field with its name, such as
  `{ type: 'InvoicePaid', invoiceId: string, amount: number }`.
- A state change slice exports one function named after its command in camel case:
  `payInvoice(history: Event[], command: PayInvoice): Event[]`. It returns the events the command appends, or throws an
  `Error` when the history rejects it. It does no I/O.
- A state view slice exports one function named after its read model in camel case, which folds the events into the
  read model: `openInvoices(events: Event[]): OpenInvoices`.
- Each specification in the build order becomes one Vitest test, named exactly like the specification: its `given`
  events are the history, its `when` is the command, and its `then` is what the test expects. Add a test for every
  other rule you write, such as a rejection.
- Strict TypeScript, no `any`, no new packages.
```

The prompt is the task for one order. The CLI fills its placeholders and adds the rules for the outcome after it:

```md title="ts-vitest/build-slice.md"
# Build {slices}

Build the {part} of {slices} in the feature `{feature}` of `{application}`. The build order is at `{order}`; this is attempt {attempt}.

1. Read the build order. Its `specifications` for these slices are the acceptance criteria: build what they describe, nothing more. `context.feedback` and `context.hints` come from people on the board, and `context.previousFailure` says why the last attempt failed.
2. Work in the folder `src/{feature}/`. When a slice's file is already there, change it in place.
3. Write each slice to `src/{feature}/<slice name>.ts` and its tests to `src/{feature}/<slice name>.test.ts`, as the instructions describe. Where the order does not say something, such as a field, decide it sensibly and list it as an assumption.
4. Run `npx tsc --noEmit`, then `npx vitest run src/{feature}`, until both pass. Run one command at a time: chained or piped commands are refused.
5. Report the result of each specification as `passed` or `failed`, from its test. Write the outcome with your file editing tool.
```

The scaffold is the files of a new project:

```json title="ts-vitest/scaffold/package.json"
{
  "name": "slices",
  "private": true,
  "type": "module",
  "scripts": {
    "typecheck": "tsc --noEmit",
    "test": "vitest run"
  },
  "devDependencies": {
    "typescript": "^7.0.0",
    "vitest": "^5.1.0"
  }
}
```

```json title="ts-vitest/scaffold/tsconfig.json"
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "noEmit": true,
    "skipLibCheck": true
  },
  "include": ["src"]
}
```

```text title="ts-vitest/scaffold/.gitignore"
node_modules/
```

## Validate and try the kit

Save the [example backend order](https://docs.cratis.studio/developers/builders/build/orders-and-outcomes/#a-build-order) as `order.json` beside the kit, then:

```bash
cratis build kit validate ./ts-vitest
mkdir shop && cd shop && git init
git commit --allow-empty -m "Start the shop"
cratis build init --kit ../ts-vitest --agent claude
git add . && git commit -m "Install the ts-vitest kit"
cratis build once --order ../order.json --agent claude --outcome ../outcome.json
```

The agent writes `src/Invoicing/PayInvoice.ts` and `src/Invoicing/PayInvoice.test.ts`, the CLI runs the type check and the tests, and commits both files as `Build PayInvoice`. The outcome lists the files, the result of each specification, and the assumptions the agent made where the order was silent:

```json
{
  "contractVersion": "1",
  "orderId": "6f0c3d5e-2b1a-4f7e-9a51-0d2c8e4b7a10",
  "kind": "built",
  "files": ["src/Invoicing/PayInvoice.ts", "src/Invoicing/PayInvoice.test.ts"],
  "specifications": [{ "id": "8e1f2a3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b", "result": "passed" }],
  "assumptions": [
    "PayInvoice identifies an invoice by invoiceId.",
    "PayInvoice pays the full outstanding amount; partial payments are not supported."
  ],
  "commit": "3c9c782223c92da32371b49d5985dc7f6e93080b",
  "branch": "main"
}
```

The code differs from run to run; what the CLI checks does not. When parts of the code should come out the same every time, generate them: see [Deterministic with AI on top](deterministic-and-ai.md).

## Write good instructions

- **Point at the specifications.** Ask for one test per specification, named like it.
- **Be concrete about the layout.** Name the folder, the file and the function, with an example such as `src/{feature}/<slice name>.ts`.
- **Let the checks decide.** Put the type check and the tests in `verify`, and tell the agent to run the same commands until they pass.
- **Permit little.** Leave out package installs and anything that reaches the network.
- **Ask for one command at a time.** Chained and piped commands are refused. Ask the agent to write the outcome with its file editing tool.
- **Ask for assumptions.** A listed assumption comes back to the slice in Studio; a silent decision does not.

## Related

- [Build with AI](https://docs.cratis.studio/developers/builders/build/with-ai/), in the Studio documentation: this sample explained line by line.
- [Deterministic with AI on top](deterministic-and-ai.md): a generator writes the structure and the agent completes it.
- [Agents and build kits](agents-and-kits.md): the agents, and the kit reference.
- [`cratis build` reference](../reference/build.md).
