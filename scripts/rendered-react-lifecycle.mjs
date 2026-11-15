// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { randomUUID, createHash } from 'node:crypto';
import { mkdirSync, mkdtempSync, readFileSync, writeFileSync, readdirSync, existsSync, createWriteStream } from 'node:fs';
import { resolve, join, delimiter, isAbsolute } from 'node:path';
import { pathToFileURL } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';

export const ownerLabel = 'io.cratis.rendered-react.owner';
const fullId = /^[a-f0-9]{64}$/;
const valueOptions = new Set(['--name', '-p', '-v', '-e', '--network', '--network-alias']);
const fail = message => { throw new Error(message); };

export const lifecycleTiming = Object.freeze({
    readinessAttempts: 120, readinessInterval: 2000,
    browserAttempts: 3000, browserInterval: 100,
    childCleanup: 122000, containerCleanup: 120000, margin: 118000
});
// One Stage run: 240s readiness + 300s browser + two 61s child reaps + 120s verified
// container cleanup + 118s scheduling margin. Normal lifecycle cleanup stops it first.
// This 900s client-only backstop must not truncate a valid 540s verification window.
export const attachedRunTimeout = lifecycleTiming.readinessAttempts * lifecycleTiming.readinessInterval
    + lifecycleTiming.browserAttempts * lifecycleTiming.browserInterval
    + lifecycleTiming.childCleanup + lifecycleTiming.containerCleanup + lifecycleTiming.margin;

// Only the forms emitted by StageContainer are supported. Image arguments are opaque.
export function parseRun(args) {
    let name;
    let index = 1;
    for (; index < args.length && args[index].startsWith('-'); index++) {
        const option = args[index];
        if (option === '--rm') continue;
        if (!valueOptions.has(option)) fail(`Unsupported Docker run option: ${option}`);
        const value = args[++index];
        if (!value || value.startsWith('-')) fail(`Missing value for ${option}`);
        if (option === '--name') name = uniqueName(name, value);
    }
    if (![args[0] === 'run', Boolean(name), Boolean(args[index])].every(Boolean)) fail('Expected named Docker run with image');
    return { name, imageIndex: index };
}

function uniqueName(previous, value) {
    if (previous || !/^[a-zA-Z0-9][a-zA-Z0-9_.-]*$/.test(value)) fail('Ambiguous container name');
    return value;
}

export function initialize(root, docker) {
    if (!isAbsolute(docker)) fail('Docker executable must be absolute');
    const state = mkdtempSync(join(root, 'live-lifecycle-'));
    mkdirSync(join(state, 'creates'), { mode: 0o700 });
    mkdirSync(join(state, 'bin'), { mode: 0o700 });
    writeFileSync(join(state, 'config.json'), JSON.stringify({ docker, owner: randomUUID() }), { mode: 0o600 });
    const moduleUrl = import.meta.url;
    writeFileSync(join(state, 'bin', 'docker'), `#!${process.execPath}\nimport(${JSON.stringify(moduleUrl)}).then(module => module.main(['docker', ${JSON.stringify(state)}, ...process.argv.slice(2)]));\n`, { mode: 0o700 });
    return state;
}

function configuration(state) {
    return JSON.parse(readFileSync(join(state, 'config.json'), 'utf8'));
}

function receiptDirectory(state, name) {
    return join(state, 'creates', createHash('sha256').update(name).digest('hex'));
}

export async function execute(executable, args, { inherit = false, timeout = 15000 } = {}) {
    const child = spawn(executable, args, { stdio: inherit ? 'inherit' : ['ignore', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';
    let timedOut = false;
    child.stdout?.on('data', data => { stdout += data; });
    child.stderr?.on('data', data => { stderr += data; });
    const timer = setTimeout(() => { timedOut = true; child.kill('SIGKILL'); }, timeout);
    try {
        const code = await new Promise((accept, reject) => {
            child.once('error', reject);
            child.once('close', status => accept(status));
        });
        if (timedOut || code === null) fail('Docker operation timed out or was signaled');
        return { code, stdout, stderr };
    } finally {
        clearTimeout(timer);
    }
}

function receipt(state, name) {
    const directory = receiptDirectory(state, name);
    if (readFileSync(join(directory, 'name'), 'utf8') !== name) fail('Receipt name mismatch');
    const id = readFileSync(join(directory, 'cid'), 'utf8').trim();
    if (!fullId.test(id)) fail('Missing or malformed full container ID receipt');
    return id;
}

// A failed inspect is never absence. A successful, structured full-ID inventory can establish it.
async function inspectOwned(state, id, run) {
    const { docker, owner } = configuration(state);
    const result = await run(docker, ['container', 'inspect', id]);
    if (result.code !== 0) {
        return await establishAbsence(docker, id, run);
    }
    const objects = JSON.parse(result.stdout);
    if (!Array.isArray(objects) || objects.length !== 1) fail('Malformed container inspection');
    const object = objects[0];
    validateIdentity(object, id, owner);
    return object;
}

function validateIdentity(object, id, owner) {
    if (object?.Id !== id) fail('Container identity mismatch');
    if (object.Config?.Labels?.[ownerLabel] !== owner) fail('Container owner mismatch');
    if (typeof object.State?.Running !== 'boolean') fail('Malformed container running state');
}

async function establishAbsence(docker, id, run) {
    const inventory = await run(docker, ['container', 'ls', '--all', '--no-trunc', '--format', '{{json .ID}}']);
    if (inventory.code !== 0) fail('Container inventory unavailable');
    const ids = inventory.stdout.trim() ? inventory.stdout.trim().split('\n').map(line => JSON.parse(line)) : [];
    if (!ids.every(item => typeof item === 'string' && fullId.test(item))) fail('Malformed container inventory');
    if (ids.includes(id)) fail('Container inspection unavailable');
    return null;
}

async function destroy(state, id, args, run) {
    if (!await inspectOwned(state, id, run)) return;
    const result = await run(configuration(state).docker, [...args, id]);
    if (result.code !== 0 && await inspectOwned(state, id, run)) fail('Owned container cleanup failed');
}

export async function dockerCommand(state, args, run = execute) {
    const { docker, owner } = configuration(state);
    if (args[0] === 'run') {
        const { name, imageIndex } = parseRun(args);
        const directory = receiptDirectory(state, name);
        mkdirSync(directory, { mode: 0o700 }); // Atomic: a name can never be reused in this task.
        writeFileSync(join(directory, 'name'), name, { mode: 0o600 });
        const injected = [...args.slice(0, imageIndex), '--label', `${ownerLabel}=${owner}`, '--cidfile', join(directory, 'cid'), ...args.slice(imageIndex)];
        return (await run(docker, injected, { inherit: true, timeout: attachedRunTimeout })).code;
    }
    if (args.length === 2 && args[0] === 'stop') {
        await destroy(state, receipt(state, args[1]), ['stop'], run);
        return 0;
    }
    if (isStatusQuery(args)) {
        const id = receipt(state, args[3].slice(6, -1));
        const object = await inspectOwned(state, id, run);
        if (object?.State.Running) process.stdout.write(`${id}\n`);
        return 0;
    }
    fail('Unsupported Docker invocation');
}

function isStatusQuery(args) {
    if (args.length !== 4) return false;
    if (args.slice(0, 3).join(' ') !== 'ps --quiet --filter') return false;
    return /^name=\^[a-zA-Z0-9][a-zA-Z0-9_.-]*\$$/.test(args[3]);
}

export async function cleanupContainers(state, run = execute) {
    let success = true;
    for (const directory of readdirSync(join(state, 'creates'))) {
        try {
            const name = readFileSync(join(state, 'creates', directory, 'name'), 'utf8');
            const id = receipt(state, name);
            await destroy(state, id, ['stop'], run);
            await destroy(state, id, ['rm', '--force'], run);
            if (await inspectOwned(state, id, run)) fail('Owned container remains after cleanup');
        } catch (error) {
            console.error(`Cleanup indeterminate; retained ${state}: ${error.message}`);
            success = false;
        }
    }
    return success;
}

export function finalStatus(original, cleaned) {
    return original !== 0 ? original : cleaned ? 0 : 1;
}

function running(child) {
    return child.exitCode === null && child.signalCode === null;
}

async function waitForExit(child, wait, attempts, interval) {
    for (let count = 0; count < attempts && running(child); count++) await wait(interval);
}

export async function reap(child, wait = delay, attempts = 60) {
    if (!running(child)) return;
    child.kill('SIGTERM');
    await waitForExit(child, wait, attempts, 1000);
    if (!running(child)) return;
    child.kill('SIGKILL');
    await waitForExit(child, wait, 10, 100);
    if (running(child)) fail('Tracked child did not exit');
}

async function awaitReady(context) {
    for (let count = 0; count < context.attempts && !context.interrupted; count++) {
        if (!running(context.child)) return false;
        if (context.ready()) return true;
        await context.wait(lifecycleTiming.readinessInterval);
    }
    return false;
}

async function awaitBrowser(context) {
    for (let count = 0; count < lifecycleTiming.browserAttempts && running(context.browser); count++) {
        if (context.interrupted || !running(context.child)) return;
        await context.wait(lifecycleTiming.browserInterval);
    }
}

function verificationStatus(context) {
    if (!running(context.child)) return context.child.exitCode || 1;
    return context.browser.exitCode ?? 1;
}

async function runVerification(context) {
    context.child = context.launch();
    const ready = await awaitReady(context);
    if (context.interrupted) return;
    if (!ready) {
        context.status = context.child.exitCode || 1;
        fail('cratis run did not become ready');
    }
    context.browser = context.verify();
    await awaitBrowser(context);
    if (!context.interrupted) context.status = verificationStatus(context);
}

function registerSignals(context) {
    const interrupt = code => { context.interrupted = true; context.status = code; };
    const onInt = () => interrupt(130);
    const onTerm = () => interrupt(143);
    context.signals.on('SIGINT', onInt);
    context.signals.on('SIGTERM', onTerm);
    return () => {
        context.signals.off('SIGINT', onInt);
        context.signals.off('SIGTERM', onTerm);
    };
}

async function finish(children, cleanup, wait) {
    let cleaned = true;
    for (const child of children.filter(Boolean)) {
        try { await reap(child, wait); } catch (error) { console.error(error.message); cleaned = false; }
    }
    try { return await cleanup() && cleaned; } catch (error) { console.error(error.message); return false; }
}

export async function supervise({ wait = delay, attempts = lifecycleTiming.readinessAttempts, signals = process, ...operations }) {
    const context = { ...operations, wait, attempts, signals, status: 0, interrupted: false };
    const unregister = registerSignals(context);
    try {
        await runVerification(context);
    } catch (error) {
        console.error(error.message);
        context.status ||= 1;
    } finally {
        const cleaned = await finish([context.browser, context.child], context.cleanup, wait);
        context.status = finalStatus(context.status, cleaned);
        unregister();
    }
    return context.status;
}

async function live(state, log, args) {
    const boundary = args.indexOf('--browser');
    if (boundary < 1 || boundary === args.length - 1) fail('Expected CLI and browser commands');
    const output = createWriteStream(log);
    await new Promise((accept, reject) => { output.once('open', accept); output.once('error', reject); });
    const start = (command, options) => {
        const child = spawn(command[0], command.slice(1), options);
        child.on('error', error => { console.error(error.message); child.exitCode = 1; });
        return child;
    };
    try {
        return await supervise({
            launch: () => start(args.slice(0, boundary), { env: { ...process.env, PATH: `${join(state, 'bin')}${delimiter}${process.env.PATH}` }, stdio: ['ignore', output, output] }),
            ready: () => existsSync(log) && readFileSync(log, 'utf8').includes('"status":"ready"'),
            verify: () => start(args.slice(boundary + 1), { stdio: 'inherit' }),
            cleanup: () => cleanupContainers(state)
        });
    } finally { output.end(); }
}

export async function main(args) {
    try {
        const [command, state, ...rest] = args;
        if (command === 'init') console.log(initialize(state, rest[0]));
        else if (command === 'docker') process.exitCode = await dockerCommand(state, rest);
        else if (command === 'cleanup') process.exitCode = await cleanupContainers(state) ? 0 : 1;
        else if (command === 'live') process.exitCode = await live(state, rest[0], rest.slice(1));
        else fail('Unknown lifecycle command');
    } catch (error) { console.error(error.message); process.exitCode = 1; }
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) await main(process.argv.slice(2));
