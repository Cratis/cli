// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EventEmitter, once } from 'node:events';
import { spawn } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { initialize, parseRun, dockerCommand, cleanupContainers, ownerLabel, finalStatus, reap, supervise, execute, lifecycleTiming, attachedRunTimeout } from './rendered-react-lifecycle.mjs';

const root = resolve('.ai-work/cli-532/fakes');
mkdirSync(root, { recursive: true });
const id = 'a'.repeat(64);
const stranger = 'b'.repeat(64);
const ok = stdout => ({ code: 0, stdout: stdout ?? '', stderr: '' });
const unavailable = { code: 1, stdout: '', stderr: 'not proof of absence' };
const runArgs = name => ['run', '--rm', '--name', name, '-p', '19391:8080', '-v', '/model:/app/model:ro', '-e', 'Stage__Runtime__Engine=semantic', 'cratis/stage:4.201.1'];

function fakeCreate(options, args) {
    const cid = args[args.indexOf('--cidfile') + 1];
    if (!options.missing) writeFileSync(cid, options.malformed ? 'short' : id);
    return options.createFailed ? unavailable : ok();
}

function fakeInspect(context) {
    if (context.options.timeout) throw new Error('fake timeout');
    if (context.options.inspectUnavailable || !context.object) return unavailable;
    return ok(JSON.stringify([context.object]));
}

function fakeInventory(context) {
    if (context.options.inventoryUnavailable) return unavailable;
    if (context.options.badInventory) return ok('{}');
    return ok(context.inventory.map(item => JSON.stringify(item)).join('\n'));
}

function fakeDestroy(context, args) {
    assert.ok(['stop', 'rm'].includes(args[0]), `unexpected forwarding: ${args}`);
    assert.equal(args.at(-1), id);
    assert.deepEqual(context.calls.at(-2), ['container', 'inspect', id]);
    assert.equal(context.object.Id, id);
    assert.equal(context.object.Config.Labels[ownerLabel], context.owner);
    mutateFakeContainer(context, args[0]);
    return context.options.destroyFailed ? unavailable : ok();
}

function mutateFakeContainer(context, command) {
    if (command === 'stop' && context.options.driftAfterStop) {
        context.object.Config.Labels[ownerLabel] = 'stranger';
        return;
    }
    if (command === 'rm' || !context.options.keepAfterStop) {
        context.object = null;
        context.inventory = [];
    }
}

function fixture(options = {}) {
    const state = initialize(root, '/fake/docker');
    const { owner } = JSON.parse(readFileSync(join(state, 'config.json'), 'utf8'));
    const context = { options, owner, calls: [], inventory: [id], object: { Id: id, Config: { Labels: { [ownerLabel]: owner } }, State: { Running: true } } };
    const handlers = {
        run: args => fakeCreate(options, args),
        inspect: () => fakeInspect(context),
        ls: () => fakeInventory(context),
        stop: args => fakeDestroy(context, args),
        rm: args => fakeDestroy(context, args)
    };
    const run = async (executable, args, executionOptions) => {
        assert.equal(executable, '/fake/docker');
        context.calls.push(args);
        const command = args[0] === 'container' ? args[1] : args[0];
        assert.ok(handlers[command], `unexpected forwarding: ${args}`);
        if (command === 'run') assert.equal(executionOptions.timeout, attachedRunTimeout);
        return handlers[command](args);
    };
    return {
        state, calls: context.calls, run,
        absent() { context.object = null; context.inventory = [stranger]; },
        mismatch() { context.object.Config.Labels[ownerLabel] = 'other-task'; },
        wrongId() { context.object.Id = stranger; },
        malformed() { context.object = {}; }
    };
}

const destructive = fixture => fixture.calls.filter(args => ['stop', 'rm', 'kill', 'prune'].includes(args[0]));
const create = fixture => dockerCommand(fixture.state, runArgs('stage-task'), fixture.run);

test('parser accepts exact source forms and preserves everything after image', () => {
    const args = [...runArgs('stage-one').slice(0, -1), '--network', 'test', '--network-alias', 'stage', 'image', '--label', `${ownerLabel}=opaque`, '--cidfile', 'opaque'];
    const parsed = parseRun(args);
    assert.equal(parsed.name, 'stage-one');
    assert.deepEqual(args.slice(parsed.imageIndex), ['image', '--label', `${ownerLabel}=opaque`, '--cidfile', 'opaque']);
});

test('runtime network, package mounts, environment and file-input grammar forward intact', async () => {
    const f = fixture();
    const args = ['run', '--rm', '--name', 'stage-runtime', '--network', 'run-network', '--network-alias', 'stage',
        '-p', '127.0.0.1:19391:8080', '-p', '127.0.0.1:19392:8081',
        '-v', '/model/input.play:/app/input.play:ro', '-v', '/seeds:/app/model:ro',
        '-v', '/packages/set.json:/app/packages.json:ro', '-v', '/packages/acme:/app/packages/acme:ro',
        '-e', 'Stage__Runtime__Engine=semantic', '-e', 'Runtime__Value=with=equals', '-v', '/runtime:/app/runtime',
        'cratis/stage:4.201.1', '/app/input.play'];
    const imageIndex = args.indexOf('cratis/stage:4.201.1');
    assert.equal(await dockerCommand(f.state, args, f.run), 0);
    assert.deepEqual(f.calls[0].slice(0, imageIndex), args.slice(0, imageIndex));
    assert.deepEqual(f.calls[0].slice(imageIndex + 4), args.slice(imageIndex));
});

test('parser rejects ambiguity, overrides, incoming cidfiles and all label forms', () => {
    for (const option of ['--label', '-l', '--label-file', '--cidfile', '--context', '-H', '--privileged', '--name=other', '--', '-it', '--publish=19391:8080']) {
        assert.throws(() => parseRun(['run', '--name', 'stage', option, 'value', 'image']));
    }
    for (const args of [[], ['run', 'image'], ['run', '--name', 'a', '--name', 'b', 'image'], ['run', '--name', 'a', '-p'], ['run', '--name', 'a', '-v', '--rm', 'image']]) {
        assert.throws(() => parseRun(args));
    }
});

test('injection is before image; opaque command and mount/env values survive exactly', async () => {
    const f = fixture();
    const args = [...runArgs('stage-task'), '/app/input.play', '--label', 'opaque'];
    assert.equal(await dockerCommand(f.state, args, f.run), 0);
    const invoked = f.calls[0];
    assert.deepEqual(invoked.slice(0, 10), args.slice(0, 10));
    assert.equal(invoked[10], '--label');
    assert.ok(invoked[11].startsWith(`${ownerLabel}=`));
    assert.equal(invoked[12], '--cidfile');
    assert.deepEqual(invoked.slice(14), args.slice(10));
    await assert.rejects(create(f), /EEXIST/);
});

test('unsupported invocations never forward any Docker operation', async () => {
    const f = fixture();
    for (const args of [['rm', '--force', 'stranger'], ['kill', id], ['prune'], ['compose', 'down'], ['--context', 'other', 'run'], ['ps', '--quiet', '--filter', 'publish=19391'], ['stop', 'stranger'], ['ps', '--quiet', '--filter', 'name=^stranger$'], ['stop', 'stage', 'other']]) {
        await assert.rejects(dockerCommand(f.state, args, f.run));
    }
    assert.deepEqual(f.calls, []);
});

test('owned fallback stop/removal use full ID, reverify, and repeat safely', async () => {
    const f = fixture({ keepAfterStop: true });
    await create(f);
    assert.equal(await cleanupContainers(f.state, f.run), true);
    assert.deepEqual(destructive(f), [['stop', id], ['rm', '--force', id]]);
    const count = destructive(f).length;
    assert.equal(await cleanupContainers(f.state, f.run), true);
    assert.equal(destructive(f).length, count);
});

test('named status/stop resolve only receipts, never names at Docker', async () => {
    const f = fixture();
    await create(f);
    assert.equal(await dockerCommand(f.state, ['ps', '--quiet', '--filter', 'name=^stage-task$'], f.run), 0);
    assert.equal(await dockerCommand(f.state, ['stop', 'stage-task'], f.run), 0);
    assert.deepEqual(destructive(f), [['stop', id]]);
});

test('occupied port and failed same-name create without receipt grant no destruction', async () => {
    for (const cause of ['occupied port', 'same-name collision']) {
        const f = fixture({ missing: true, createFailed: true });
        assert.equal(await create(f), 1, cause);
        assert.equal(await cleanupContainers(f.state, f.run), false, cause);
        assert.deepEqual(destructive(f), []);
    }
});

test('partial create failure with receipt still cleans only verified owned ID', async () => {
    const f = fixture({ createFailed: true });
    assert.equal(await create(f), 1);
    assert.equal(await cleanupContainers(f.state, f.run), true);
    assert.deepEqual(destructive(f), [['stop', id]]);
});

test('missing/malformed receipts and mismatched owner/ID/inspection fail closed', async () => {
    for (const scenario of ['missing', 'malformed', 'mismatch', 'wrongId', 'malformedObject']) {
        const f = fixture({ missing: scenario === 'missing', malformed: scenario === 'malformed' });
        await create(f);
        if (scenario === 'mismatch') f.mismatch();
        if (scenario === 'wrongId') f.wrongId();
        if (scenario === 'malformedObject') f.malformed();
        assert.equal(await cleanupContainers(f.state, f.run), false, scenario);
        assert.deepEqual(destructive(f), [], scenario);
    }
});

test('later stranger and concurrent task cannot acquire receipt authority', async () => {
    const first = fixture();
    const second = fixture();
    await Promise.all([create(first), create(second)]);
    assert.notEqual(readFileSync(join(first.state, 'config.json'), 'utf8'), readFileSync(join(second.state, 'config.json'), 'utf8'));
    assert.equal(await cleanupContainers(first.state, second.run), false);
    first.absent(); // Original ID gone; name now occupied by a stranger with another ID.
    assert.equal(await cleanupContainers(first.state, first.run), true);
    assert.deepEqual(destructive(first), []);
    assert.deepEqual(destructive(second), []);
});

test('absence requires successful structured inventory; unavailable/malformed/timeouts fail', async () => {
    for (const options of [{ inspectUnavailable: true }, { inspectUnavailable: true, inventoryUnavailable: true }, { inspectUnavailable: true, badInventory: true }, { timeout: true }]) {
        const f = fixture(options);
        await create(f);
        assert.equal(await cleanupContainers(f.state, f.run), false);
        assert.deepEqual(destructive(f), []);
    }
    const absent = fixture();
    await create(absent);
    absent.absent();
    assert.equal(await cleanupContainers(absent.state, absent.run), true);
    assert.deepEqual(destructive(absent), []);
});

test('owner drift between stop and forced fallback blocks removal', async () => {
    const f = fixture({ keepAfterStop: true, driftAfterStop: true });
    await create(f);
    assert.equal(await cleanupContainers(f.state, f.run), false);
    assert.deepEqual(destructive(f), [['stop', id]]);
});

test('destructive failure cannot report successful cleanup', async () => {
    const f = fixture({ keepAfterStop: true, destroyFailed: true });
    await create(f);
    assert.equal(await cleanupContainers(f.state, f.run), false);
    assert.deepEqual(destructive(f), [['stop', id]]);
});

function child(mode = 'graceful') {
    return {
        exitCode: null, signalCode: null, signals: [],
        kill(signal) {
            this.signals.push(signal);
            if (mode === 'graceful' || signal === 'SIGKILL') this.signalCode = signal;
        }
    };
}

test('reap signals tracked child only: graceful TERM or bounded forced KILL', async () => {
    const graceful = child();
    await reap(graceful, async () => {});
    assert.deepEqual(graceful.signals, ['SIGTERM']);
    const forced = child('forced');
    let waits = 0;
    await reap(forced, async () => { waits++; }, 3);
    assert.deepEqual(forced.signals, ['SIGTERM', 'SIGKILL']);
    assert.equal(waits, 3);
    await reap(forced, async () => {});
    assert.deepEqual(forced.signals, ['SIGTERM', 'SIGKILL']);
});

async function scenario({ signal, early = false, cleaned = true, browserStatus = 0, forced = false, launchFailure = false } = {}) {
    const signals = new EventEmitter();
    const cli = child(forced ? 'forced' : 'graceful');
    const browser = child();
    browser.exitCode = browserStatus;
    let cleanedCount = 0;
    let launched = false;
    const status = await supervise({
        signals, attempts: 2,
        launch: () => {
            assert.equal(signals.listenerCount('SIGINT'), 1);
            assert.equal(signals.listenerCount('SIGTERM'), 1);
            if (launchFailure) throw new Error('fake launch failure');
            launched = true;
            if (early) cli.exitCode = 7;
            return cli;
        },
        ready: () => !signal && !early,
        verify: () => browser,
        cleanup: async () => { cleanedCount++; return cleaned; },
        wait: async () => { if (signal) signals.emit(signal); }
    });
    assert.equal(signals.listenerCount('SIGINT'), 0);
    assert.equal(signals.listenerCount('SIGTERM'), 0);
    assert.equal(cleanedCount, 1);
    assertScenarioSignals({ launched, early, forced, cli });
    return status;
}

function assertScenarioSignals({ launched, early, forced, cli }) {
    if (!launched || early) return;
    assert.deepEqual(cli.signals, forced ? ['SIGTERM', 'SIGKILL'] : ['SIGTERM']);
}

test('early EXIT/launch failure still cleans; original nonzero survives cleanup failure', async () => {
    assert.equal(await scenario({ early: true, cleaned: false }), 7);
    assert.equal(await scenario({ launchFailure: true }), 1);
    assert.equal(await scenario({ browserStatus: 2, cleaned: false }), 2);
    assert.equal(await scenario({ cleaned: false }), 1);
    assert.equal(await scenario({ forced: true }), 0);
});

test('INT/TERM handlers installed before launch, reap and preserve 130/143', async () => {
    assert.equal(await scenario({ signal: 'SIGINT', cleaned: false }), 130);
    assert.equal(await scenario({ signal: 'SIGTERM', cleaned: false, forced: true }), 143);
});

test('bounded readiness timeout and thrown cleanup are nonzero', async () => {
    const cli = child();
    let polls = 0;
    const status = await supervise({ launch: () => cli, ready: () => false, verify: () => assert.fail('not ready'), cleanup: () => { throw new Error('fake unavailable'); }, wait: async () => { polls++; }, attempts: 3 });
    assert.equal(status, 1);
    assert.equal(polls, 3);
    assert.deepEqual(cli.signals, ['SIGTERM']);
});

test('attached Docker deadline covers readiness, browser, child and container cleanup', () => {
    const verification = lifecycleTiming.readinessAttempts * lifecycleTiming.readinessInterval
        + lifecycleTiming.browserAttempts * lifecycleTiming.browserInterval;
    assert.equal(verification, 540000);
    assert.equal(lifecycleTiming.childCleanup, 2 * 61000);
    assert.equal(attachedRunTimeout, verification + lifecycleTiming.childCleanup + lifecycleTiming.containerCleanup + lifecycleTiming.margin);
    assert.equal(attachedRunTimeout, 900000);
});

test('unexpected live CLI exit after readiness cannot be hidden by successful browser', async () => {
    for (const cliExit of [0, 9]) {
        const cli = child();
        const browser = child();
        browser.exitCode = 0;
        const status = await supervise({
            launch: () => cli, ready: () => true,
            verify: () => { cli.exitCode = cliExit; return browser; },
            cleanup: async () => true, wait: async () => {}
        });
        assert.equal(status, cliExit || 1);
    }
});

test('live CLI exit while browser is pending preserves failure when browser later returns zero', async () => {
    const cli = child();
    const browser = child();
    const status = await supervise({
        launch: () => cli, ready: () => true, verify: () => browser, cleanup: async () => true,
        wait: async () => { cli.exitCode = 9; browser.exitCode = 0; }
    });
    assert.equal(status, 9);
});

test('stale ready log from exited CLI does not launch browser', async () => {
    const cli = child();
    cli.exitCode = 0;
    assert.equal(await supervise({ launch: () => cli, ready: () => true, verify: () => assert.fail('CLI already exited'), cleanup: async () => true, wait: async () => {} }), 1);
});

test('generated shim propagates actual fake process early exit and spawn error without receipt authority', async () => {
    const directory = initialize(root, '/fake/unused');
    const early = join(directory, 'early-docker');
    writeFileSync(early, `#!${process.execPath}\nprocess.exitCode = 9;\n`, { mode: 0o700 });
    for (const executable of [early, join(directory, 'missing-docker')]) {
        const state = initialize(directory, executable);
        const wrapper = join(state, 'bin', 'docker');
        let fake;
        let closed;
        let stderr = '';
        const status = await supervise({
            launch: () => {
                fake = spawn(wrapper, runArgs('stage-task'), { stdio: ['ignore', 'pipe', 'pipe'] });
                closed = once(fake, 'close');
                fake.stderr.on('data', data => { stderr += data; });
                return fake;
            },
            ready: () => false, verify: () => assert.fail('fake Docker failed before ready'),
            cleanup: () => cleanupContainers(state), attempts: 4000,
            wait: () => new Promise(accept => setTimeout(accept, 2))
        });
        assert.equal(status, executable === early ? 9 : 1);
        if (executable !== early) assert.match(stderr, /ENOENT/);
        await closed;
        assert.equal(fake.signalCode, null);
    }
});

test('executable wrapper exercises a fake Docker process, never the host daemon', async () => {
    const directory = initialize(root, '/fake/unused');
    const executable = join(directory, 'fake-docker');
    const model = join(directory, 'fake-model.json');
    const log = join(directory, 'fake-argv.json');
    writeFileSync(executable, `#!${process.execPath}
const fs = require('node:fs');
const args = process.argv.slice(2);
const model = ${JSON.stringify(model)};
const log = ${JSON.stringify(log)};
const id = ${JSON.stringify(id)};
const calls = fs.existsSync(log) ? JSON.parse(fs.readFileSync(log)) : [];
calls.push(args); fs.writeFileSync(log, JSON.stringify(calls));
let object = fs.existsSync(model) ? JSON.parse(fs.readFileSync(model)) : null;
if (args[0] === 'run') {
    const label = args[args.indexOf('--label') + 1].split('=');
    object = { Id: id, Config: { Labels: { [label[0]]: label[1] } }, State: { Running: true } };
    fs.writeFileSync(args[args.indexOf('--cidfile') + 1], id);
} else if (args[1] === 'inspect') {
    if (object) console.log(JSON.stringify([object])); else process.exitCode = 1;
} else if (args[1] === 'ls') {
    if (object) console.log(JSON.stringify(id));
} else if (args[0] === 'stop') object.State.Running = false;
else if (args[0] === 'rm') object = null;
else process.exitCode = 90;
fs.writeFileSync(model, JSON.stringify(object));
`, { mode: 0o700 });
    const state = initialize(directory, executable);
    const wrapper = join(state, 'bin', 'docker');
    assert.equal((await execute(wrapper, runArgs('stage-task'))).code, 0);
    assert.equal((await execute(wrapper, ['stop', 'stage-task'])).code, 0);
    assert.equal(await cleanupContainers(state), true);
    const calls = JSON.parse(readFileSync(log, 'utf8'));
    assert.deepEqual(calls.filter(args => ['stop', 'rm'].includes(args[0])), [['stop', id], ['stop', id], ['rm', '--force', id]]);
    const denied = await execute(wrapper, ['rm', '--force', 'unknown']);
    assert.equal(denied.code, 1);
    assert.match(denied.stderr, /Unsupported Docker invocation/);
    assert.equal(JSON.parse(readFileSync(log, 'utf8')).length, calls.length);
});

test('signals during cleanup keep their original status', async () => {
    const signals = new EventEmitter();
    const cli = child();
    const browser = child();
    browser.exitCode = 0;
    assert.equal(await supervise({ signals, launch: () => cli, ready: () => true, verify: () => browser, cleanup: async () => { signals.emit('SIGTERM'); return false; }, wait: async () => {} }), 143);
});

test('status precedence includes signals and original scenario defects', () => {
    for (const original of [0, 1, 2, 7, 130, 143]) {
        assert.equal(finalStatus(original, true), original);
        assert.equal(finalStatus(original, false), original || 1);
    }
});
