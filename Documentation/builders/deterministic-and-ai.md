---
title: Generate slices and let a coding agent complete them
description: A generator writes the structure of every slice from the build order, and a coding agent fills in only the marked places. A complete cratis build sample.
---

A generator is good at what follows a rule: file names, type names, function signatures, one test per specification. A coding agent is good at what needs judgment: the fields of an event, the business rule, the data a test needs. **Deterministic with AI on top** splits a build between them: the generator writes the predictable part and marks every place that needs judgment, the agent fills in only those places, and the `verify` commands decide whether the result is committed.

The Studio documentation has the long walkthrough: [Deterministic with AI on top](https://docs.cratis.studio/developers/builders/build/deterministic-and-ai/).

## How it fits together

It is one agent build, run by `cratis build` with a [build kit](agents-and-kits.md#build-kits). The kit carries the generator, and:

- **permits the generator as a command**, so the agent may run it;
- **tells the agent, in its prompt, to run the generator first** and then fill in only the marked places, passing it the path of the order with the `{order}` placeholder;
- **verifies with the generator itself**, in a check mode that fails while a file, a test or a marked place is missing, followed by the type check and the tests.

The generator never overwrites a file, so running it again on a later order leaves the agent's work alone.

## Sample: a generator and an agent for TypeScript

This kit builds state change slices in plain TypeScript on Node.js, tested with Vitest. Make a folder `ts-vitest-generated` with these files:

```text
ts-vitest-generated/
├── build-kit.yaml           # the manifest
├── AGENTS.md                # the stack instructions
├── build-slice.md           # the prompt for one build order
├── generator/
│   └── generate.mts         # the slice generator
└── scaffold/                # the files of a new project, as in Build with AI
    ├── .gitignore
    ├── package.json
    └── tsconfig.json
```

The `scaffold` folder holds the same three files as in [the Build with AI sample](build-with-ai.md#sample-a-kit-of-your-own).

### The generator

For every state change slice, the generator writes `src/<feature>/<slice>.ts` and `src/<feature>/<slice>.test.ts` when they are not there yet, with names taken from the specifications, and leaves `TODO(agent)` where the agent works. With `--check`, it changes nothing and fails while any file, test or `TODO(agent)` is missing. Node.js 22.18 or later runs it as it is.

```ts title="ts-vitest-generated/generator/generate.mts"
// The slice generator: writes the predictable part of every state change slice in a build order,
// with TODO(agent) where judgment is needed. With --check, it fails while any of that is missing.
//   node generate.mts <order.json>          write what is missing, never overwrite
//   node generate.mts --check <order.json>  exit 1 while a file, a test or a filled-in place is missing
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';

type Slice = { id: string; name: string; type: string };
type Specification = { sliceId: string; name: string; given: string[]; when: string; then: string[] };
type Order = { scope: { feature: { name: string }; slices: Slice[] }; specifications: Specification[] };

const check = process.argv.includes('--check');
const orderFile = process.argv.slice(2).find(argument => argument !== '--check') ?? process.env.STUDIO_BUILD_ORDER;
if (!orderFile) throw new Error('Name the build order: node generate.mts <order.json>');
const order: Order = JSON.parse(readFileSync(orderFile, 'utf8'));

const header = 'Generated from the build order by the slice generator.';
const todo = 'TODO(agent)';
const safe = (name: string) => /^[A-Za-z][A-Za-z0-9]*$/.test(name);
// "event Invoice Issued" -> "InvoiceIssued"; "command Pay Invoice" -> "PayInvoice".
const typeName = (element: string) => element.replace(/^(event|command)\s+/, '').split(/\s+/).map(word => word[0].toUpperCase() + word.slice(1)).join('');
const camel = (name: string) => name[0].toLowerCase() + name.slice(1);
const isEvent = (element: string) => element.startsWith('event ');

function source(slice: Slice, command: string, events: string[]): string {
    const lines = [`// ${header}`, `// ${slice.name}: a state change in ${order.scope.feature.name}. Keep the names; fill in only the marked places.`, ''];
    for (const event of events) {
        lines.push(`export type ${event} = {`, `    type: '${event}';`, `    // ${todo}: the fields of ${event}.`, '};', '');
    }
    lines.push(
        `export type Event = ${events.length > 0 ? events.join(' | ') : 'never'};`, '',
        `export type ${command} = {`, `    // ${todo}: the fields of ${command}.`, '};', '',
        `export function ${camel(command)}(history: Event[], command: ${command}): Event[] {`,
        `    // ${todo}: decide from the history which events ${command} appends, or throw an Error to reject it.`,
        `    throw new Error('${command} is not built yet.');`,
        '}');
    return lines.join('\n') + '\n';
}

function test(slice: Slice, command: string, specifications: Specification[]): string {
    const lines = [
        `// ${header}`,
        `// One test per specification of ${slice.name}, named like it. Fill in only the marked places.`,
        `import { describe, expect, it } from 'vitest';`,
        'import {', `    ${camel(command)}, type Event, type ${command}`, `} from './${slice.name}';`, '',
        `describe('${slice.name}', () => {`];
    for (const [index, specification] of specifications.entries()) {
        if (index > 0) lines.push('');
        lines.push(
            `    // Given ${specification.given.join(', ') || 'nothing'}; when ${specification.when}; then ${specification.then.join(', ')}.`,
            `    it('${specification.name.replace(/'/g, "\\'")}', () => {`,
            `        const history: Event[] = [${specification.given.length > 0 ? `/* ${todo}: ${specification.given.map(typeName).join(', ')} */` : ''}];`,
            `        const command: ${command} = { /* ${todo} */ };`,
            specification.then.every(isEvent)
                ? `        expect(${camel(command)}(history, command)).toEqual([/* ${todo}: ${specification.then.map(typeName).join(', ')} */]);`
                : `        expect(() => ${camel(command)}(history, command)).toThrow(); // ${todo}: ${specification.then.join(', ')}`,
            '    });');
    }
    lines.push('});');
    return lines.join('\n') + '\n';
}

const feature = order.scope.feature.name;
if (!safe(feature)) throw new Error(`The feature name '${feature}' cannot be used as a folder name.`);
const problems: string[] = [];

for (const slice of order.scope.slices) {
    if (slice.type !== 'stateChange') throw new Error(`The generator only writes state change slices, not ${slice.name}.`);
    if (!safe(slice.name)) throw new Error(`The slice name '${slice.name}' cannot be used as a file name.`);
    const specifications = order.specifications.filter(specification => specification.sliceId === slice.id);
    const elements = specifications.flatMap(specification => [...specification.given, ...specification.then]);
    const events = [...new Set(elements.filter(isEvent).map(typeName))];
    const command = specifications[0] ? typeName(specifications[0].when) : slice.name;
    const files = {
        [`src/${feature}/${slice.name}.ts`]: source(slice, command, events),
        [`src/${feature}/${slice.name}.test.ts`]: test(slice, command, specifications)
    };

    for (const [file, content] of Object.entries(files)) {
        const existing = existsSync(file) ? readFileSync(file, 'utf8') : undefined;
        if (check) {
            if (existing === undefined) problems.push(`${file} is missing.`);
            else if (!existing.includes(header)) problems.push(`${file} lost its generated header.`);
            else if (existing.includes(todo)) problems.push(`${file} still has places marked ${todo}.`);
        } else if (existing === undefined) {
            mkdirSync(`src/${feature}`, { recursive: true });
            writeFileSync(file, content);
            console.log(`Wrote ${file}`);
        } else {
            console.log(`Kept ${file}, which is already there.`);
        }
    }

    const tests = existsSync(`src/${feature}/${slice.name}.test.ts`) ? readFileSync(`src/${feature}/${slice.name}.test.ts`, 'utf8') : '';
    for (const specification of specifications.filter(specification => !tests.includes(`it('${specification.name.replace(/'/g, "\\'")}'`))) {
        const message = `src/${feature}/${slice.name}.test.ts has no test named '${specification.name}'.`;
        if (check) problems.push(message); else console.log(`Add a test: ${message}`);
    }
}

if (problems.length > 0) {
    console.error(problems.join('\n'));
    process.exit(1);
}
```

For the [example backend order](https://docs.cratis.studio/developers/builders/build/orders-and-outcomes/#a-build-order), it writes this source file, which already compiles:

```ts title="src/Invoicing/PayInvoice.ts"
// Generated from the build order by the slice generator.
// PayInvoice: a state change in Invoicing. Keep the names; fill in only the marked places.

export type InvoiceIssued = {
    type: 'InvoiceIssued';
    // TODO(agent): the fields of InvoiceIssued.
};

export type InvoicePaid = {
    type: 'InvoicePaid';
    // TODO(agent): the fields of InvoicePaid.
};

export type Event = InvoiceIssued | InvoicePaid;

export type PayInvoice = {
    // TODO(agent): the fields of PayInvoice.
};

export function payInvoice(history: Event[], command: PayInvoice): Event[] {
    // TODO(agent): decide from the history which events PayInvoice appends, or throw an Error to reject it.
    throw new Error('PayInvoice is not built yet.');
}
```

and this test file, with one test per specification:

```ts title="src/Invoicing/PayInvoice.test.ts"
// Generated from the build order by the slice generator.
// One test per specification of PayInvoice, named like it. Fill in only the marked places.
import { describe, expect, it } from 'vitest';
import {
    payInvoice, type Event, type PayInvoice
} from './PayInvoice';

describe('PayInvoice', () => {
    // Given event Invoice Issued; when command Pay Invoice; then event Invoice Paid.
    it('Paying an open invoice', () => {
        const history: Event[] = [/* TODO(agent): InvoiceIssued */];
        const command: PayInvoice = { /* TODO(agent) */ };
        expect(payInvoice(history, command)).toEqual([/* TODO(agent): InvoicePaid */]);
    });
});
```

### The manifest

Compared with the [Build with AI](build-with-ai.md) kit, it builds only state changes, verifies with the generator's check first, and permits the generator as a command. A permitted command is a prefix, so the arguments after it are allowed:

```yaml title="ts-vitest-generated/build-kit.yaml"
kitVersion: "1"
name: ts-vitest-generated
version: 1.0.0
description: TypeScript slices on Node.js whose structure a generator writes and a coding agent completes.
sliceTypes: [stateChange]
scaffold:
  path: ./scaffold
  commands:
    - npm install
instructions: ./AGENTS.md
prompt: ./build-slice.md
verify:
  - node .cratis/build/kit/generator/generate.mts --check {order}
  - npx tsc --noEmit
  - npx vitest run src/{feature}
git:
  commit: "Build {slices}"
permissions:
  paths: ["src/**"]
  commands:
    - node .cratis/build/kit/generator/generate.mts
    - npx tsc --noEmit
    - npx vitest run
requires:
  - command: node
    version: ">=22.18"
  - command: npm
```

The commands name the generator where `cratis build init` installs it, `.cratis/build/kit/generator/generate.mts`. Install the kit with `init` rather than passing `--kit <folder>` to a build, or that path does not exist.

### The instructions and the prompt

```md title="ts-vitest-generated/AGENTS.md"
# TypeScript slices, generated and completed

This project is a set of event-sourced slices in plain TypeScript on Node.js. A generator writes the structure of every
slice; you complete it.

- The slice generator, `node .cratis/build/kit/generator/generate.mts <order>`, writes `src/<feature>/<slice>.ts` and
  `src/<feature>/<slice>.test.ts` for every slice in a build order. It decides the file names, the type and function
  names, and one test per specification. It never overwrites a file.
- Every place that needs judgment is marked `TODO(agent)`: the fields of each event and command, the body of the
  function that decides which events a command appends, and the data and expectations of each test.
- Fill in those places and remove the marker. Change nothing else: keep the generated header comments, the names, the
  test names, and the files where they are. Do not add files.
- An event has a `type` field with its name. The decide function returns the events the command appends, or throws an
  `Error` when the history rejects it. It does no I/O.
- Strict TypeScript, no `any`, no new packages.
```

```md title="ts-vitest-generated/build-slice.md"
# Build {slices}

Build {slices} in the feature `{feature}` of `{application}`. The build order is at `{order}`; this is attempt {attempt}.

1. Run the slice generator first: `node .cratis/build/kit/generator/generate.mts {order}`. It prints the files it wrote or kept, and any test that is missing.
2. Fill in every place marked `TODO(agent)` in those files, as the instructions describe. Add a test the generator says is missing, named exactly like its specification.
3. The order's `specifications` are the acceptance criteria. `context.feedback` and `context.hints` come from people on the board, and `context.previousFailure` says why the last attempt failed. Where the order does not say something, such as a field, decide it sensibly and list it as an assumption.
4. Run `npx tsc --noEmit`, then `npx vitest run src/{feature}`, then `node .cratis/build/kit/generator/generate.mts --check {order}`, until all three pass. Run one command at a time: chained or piped commands are refused.
5. Report the result of each specification as `passed` or `failed`, from its test. Write the outcome with your file editing tool.
```

## Validate and try the kit

With the example order saved as `order.json` beside the kit:

```bash
cratis build kit validate ./ts-vitest-generated
mkdir shop && cd shop && git init
git commit --allow-empty -m "Start the shop"
cratis build init --kit ../ts-vitest-generated --agent claude
git add . && git commit -m "Install the ts-vitest-generated kit"
cratis build once --order ../order.json --agent claude --outcome ../outcome.json
```

In `git show`, the header comments, names and test names are as the generator wrote them; the fields, the rule and the test data are the agent's.

## Why the split works

- **The structure never drifts.** Every slice has the same files, names and test layout, because the agent does not decide them.
- **The agent works on less.** It fills two short files with clear gaps instead of designing a slice.
- **The checks know the structure.** The generator's check catches a missing test or a skipped gap, which a type check and a test run cannot see.
- **It grows with you.** When the agent writes the same thing on every slice, move it into the generator and mark less.

## Related

- [Deterministic with AI on top](https://docs.cratis.studio/developers/builders/build/deterministic-and-ai/), in the Studio documentation: why the split works, how to tighten it, and the inverse, where the agent writes and deterministic checks decide.
- [Build with AI](build-with-ai.md): the agent writes all the code.
- [Write a generator](write-a-generator.mdx): fully deterministic, no agent.
- [Agents and build kits](agents-and-kits.md): the agents, and the kit reference.
