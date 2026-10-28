// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Browser scenario for the application shell fixture (model/application.play): navigation regions across three modules (Home inherits the
// application's sidebar, Sales overrides it, Support extends it), a toolbar button that invokes a State Change slice as a task dialog, deep
// links, and no sample data in what ships. The same scenario runs against a live `cratis run` Stage and against the application
// `cratis render` generated, because both draw the shell with Scene's navigation regions and dialog host.
//
//   SHELL_URL       the base URL of the running application (required)
//   SHELL_MODE      'live' (a Stage serving /stage/scene) or 'generated' (a rendered application; SHELL_APPLICATION is its folder)
//   SHELL_RESULT    where to write the result JSON
//   SHELL_ROOT      'own' (the model's own root template, the default) or 'blueprint' (the same shell taken from the default blueprint)
//   SHELL_SCREENSHOTS  optional folder for screenshots
//
// Exit codes: 0 every assertion passed, 1 an assertion failed, 2 the scenario could not run.

const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');

// Stage's generated-React harness (STAGE_GENERATED_REACT_SCENARIO) provides the STAGE_GENERATED_REACT_* variables.
const base = process.env.SHELL_URL ?? process.env.STAGE_GENERATED_REACT_URL;
const mode = process.env.SHELL_MODE ?? (process.env.STAGE_GENERATED_REACT_URL ? 'generated' : 'live');
const application = process.env.SHELL_APPLICATION ?? process.env.STAGE_GENERATED_REACT_APPLICATION;
const resultPath = process.env.SHELL_RESULT ?? process.env.STAGE_GENERATED_REACT_RESULT ?? 'shell-navigation-result.json';
const shots = process.env.SHELL_SCREENSHOTS ?? (mode === 'generated' ? path.dirname(resultPath) : undefined);
const sampleRow = 'must never ship';
const rootKind = process.env.SHELL_ROOT ?? 'own';

const passed = [];
const failures = [];
// JSON objects are unordered: compare with sorted keys so a renderer that emits fields in another order still matches.
const canonical = value => Array.isArray(value)
    ? value.map(canonical)
    : value !== null && typeof value === 'object'
        ? Object.fromEntries(Object.keys(value).sort().map(key => [key, canonical(value[key])]))
        : value;

const check = (name, actual, expected) => {
    const same = JSON.stringify(canonical(actual)) === JSON.stringify(canonical(expected));
    (same ? passed : failures).push(same ? name : `${name}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
};

const files = directory => fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(directory, entry.name);
    return entry.isDirectory() ? (entry.name === 'node_modules' || entry.name === 'obj' || entry.name === 'bin' ? [] : files(full)) : [full];
});

(async () => {
    let exitCode = 2;
    let browser;
    try {
        if (!base) throw new Error('SHELL_URL is required');

        if (mode === 'live') {
            const scene = await (await fetch(`${base}/stage/scene`)).json();
            check('the served scene carries no sample data packs', [scene.sampleDataPacks, scene.blueprintSampleDataPacks], [[], []]);
            check('the served scene carries no sample data selection in a profile', scene.uiProfiles.every(profile => profile.sampleData == null), true);
            check('the served scene carries no sample row', JSON.stringify(scene).includes(sampleRow), false);
        } else {
            if (!application) throw new Error('SHELL_APPLICATION is required in generated mode');
            const shipped = ['scene.json', path.join('src', 'stage-scene.json')].map(file => JSON.parse(fs.readFileSync(path.join(application, file), 'utf8')));
            check('the rendered scene documents carry no sample data packs', shipped.map(scene => [scene.sampleDataPacks ?? [], scene.blueprintSampleDataPacks ?? []]), [[[], []], [[], []]]);
            check('the rendered scene documents carry no sample data selection', shipped.every(scene => scene.uiProfiles.every(profile => profile.sampleData == null)), true);
            const shippedFiles = files(application).filter(file => /\.(js|json|html|css|cs|ts|tsx)$/.test(file));
            check('no file of the rendered application or its built frontend carries a sample row', shippedFiles.filter(file => fs.readFileSync(file, 'utf8').includes(sampleRow)).map(file => path.relative(application, file)), []);
        }

        browser = await chromium.launch();
        const page = await browser.newPage({ viewport: { width: 1500, height: 900 } });
        const errors = [];
        page.on('pageerror', error => errors.push(String(error)));
        const posts = [];
        page.on('request', request => {
            if (request.method() === 'POST' && request.url().includes('/api/')) {
                posts.push({ url: new URL(request.url()).pathname, body: JSON.parse(request.postData() || '{}') });
            }
        });
        const items = async (target, label) => (await target.locator(`nav[aria-label="${label}"] a, nav[aria-label="${label}"] button`).allInnerTexts()).map(text => text.trim());
        const active = async label => (await page.locator(`nav[aria-label="${label}"] [aria-current]`).allInnerTexts()).map(text => text.trim());
        const shot = async name => { if (shots) await page.screenshot({ path: path.join(shots, `${mode}-${name}.png`) }); };
        // A live Stage addresses screens with regular routes (`/OrderScreen`); a rendered application keeps the fragment (`#/OrderScreen`).
        const address = hash => mode === 'live' ? hash.replace(/^#/, '') : hash;
        const goTo = async (region, label, hash) => {
            await page.locator(`nav[aria-label="${region}"]`).getByText(label, { exact: true }).click();
            await page.waitForFunction(([expected, live]) => (live ? location.pathname : location.hash).startsWith(expected), [address(hash), mode === 'live']);
        };

        await page.goto(base);
        await page.waitForSelector('.layout-topbar');
        check('the topbar shows the application items', await items(page, 'Modules'), ['Home', 'Sales', 'Support']);
        check('the logo is the application\'s', (await page.locator('.layout-logo').first().innerText()).includes('Acme'), true);
        check('a module that declares nothing inherits the application sidebar', await items(page, 'Sidebar'), ['Overview']);
        check('the module is active in the topbar', await active('Modules'), ['Home']);
        await shot('home');

        // The profile's root template, shell options and appearance (Cratis/Stage#466): the shell is drawn without its footer, in the dark scheme,
        // with the accent as the theme's primary colour. SHELL_ROOT names which root the run started with ('own' or 'blueprint').
        check(`the ${rootKind} root template is drawn as the application shell`, await page.locator('.layout-wrapper').count() > 0, true);
        check('the showFooter option leaves the footer out', await page.locator('.layout-footer').count(), 0);
        check('the dark colour scheme is applied', await page.locator('body').getAttribute('data-scene-color-scheme'), 'dark');
        check('the accent is the theme\'s primary colour', (await page.locator('[data-scene-theme-root]').first().getAttribute('style') ?? '').includes('--scene-primary-color: #e11d48'), true);

        await goTo('Modules', 'Sales', '#/OrderScreen');
        check('a module that overrides the sidebar shows only its own items', await items(page, 'Sidebar'), ['Orders', 'Customers']);
        check('the module is active in the topbar once it is entered', await active('Modules'), ['Sales']);
        check('the feature is active in the sidebar', await active('Sidebar'), ['Orders']);
        await shot('sales');

        await page.getByRole('button', { name: 'New order' }).waitFor();
        await page.getByRole('button', { name: 'New order' }).click();
        const dialog = page.locator('.scene-slice-dialog');
        await dialog.waitFor();
        check('the toolbar button opens the slice as a task dialog', [await dialog.locator('h2').innerText(), await dialog.locator('.scene-slice-dialog-subtitle').innerText()], ['New order', 'State Change · PlaceOrder']);
        check('the dialog fields are the command\'s', await dialog.locator('input').evaluateAll(inputs => inputs.map(input => input.name)), ['orderId', 'customer', 'quantity']);
        await shot('dialog');
        await dialog.getByRole('button', { name: 'Cancel' }).click();
        await page.locator('[role=dialog]').waitFor({ state: 'detached' });
        check('cancelling the dialog posts nothing', posts, []);

        await page.getByRole('button', { name: 'New order' }).click();
        await dialog.waitFor();
        await dialog.locator('input[name=orderId]').fill('11111111-1111-4111-8111-111111111111');
        await dialog.locator('input[name=customer]').fill('Fjordline AS');
        await dialog.locator('input[name=quantity]').fill('4');
        await dialog.locator('button[type=submit]').click();
        await page.locator('[role=dialog]').waitFor({ state: 'detached' });
        check('submitting posts the command with what was typed', posts.map(post => post.body), [{ orderId: '11111111-1111-4111-8111-111111111111', customer: 'Fjordline AS', quantity: 4 }]);
        check('the command goes to the application\'s route', /^\/api\/sales\/orders(\/|$)/.test(posts[0]?.url ?? ''), true);

        const link = page.url();
        const second = await browser.newPage({ viewport: { width: 1500, height: 900 } });
        await second.goto(link);
        await second.waitForSelector('.layout-topbar');
        check('a deep link restores the module sidebar', await items(second, 'Sidebar'), ['Orders', 'Customers']);
        check('a deep link restores the page', (await second.locator('[data-scene-outlet="primary"] h1').allInnerTexts()).includes('Orders'), true);
        check('a deep link restores the active feature', (await second.locator('nav[aria-label="Sidebar"] [aria-current]').allInnerTexts()).map(text => text.trim()), ['Orders']);
        await second.close();

        await goTo('Sidebar', 'Customers', '#/CustomerScreen');
        check('another feature of the module becomes active', await active('Sidebar'), ['Customers']);
        await page.goBack();
        await page.waitForFunction(live => (live ? location.pathname : location.hash).startsWith(live ? '/OrderScreen' : '#/OrderScreen'), mode === 'live');
        check('back returns to the previous feature', await active('Sidebar'), ['Orders']);

        await goTo('Modules', 'Support', '#/TicketScreen');
        check('a module that extends the sidebar shows the application\'s items then its own', await items(page, 'Sidebar'), ['Overview', 'Tickets', 'Knowledge base']);
        check('the extended module is active', await active('Modules'), ['Support']);
        await shot('support');

        // An item authored with `slot side` renders into the slot the shell declares (Cratis/Stage#456). The own root template is the model's
        // layout, which declares `side`; the default blueprint's AppShell has no such slot, so the item opens its target in the primary outlet.
        await page.locator('nav[aria-label="Sidebar"]').getByText('Knowledge base', { exact: true }).click();
        if (rootKind === 'own') {
            const sidePane = page.locator('[data-scene-outlet="side"]');
            await sidePane.getByText('Knowledge base', { exact: false }).first().waitFor();
            check('the page stays in the primary outlet when a slot item is opened', await page.locator('[data-scene-outlet="primary"]').getAttribute('data-scene-screen'), 'TicketScreen');
            check('the slot item renders its target in the side pane', (await sidePane.innerText()).includes('Knowledge base'), true);
            check('both the page and the slot item are active', await active('Sidebar'), ['Tickets', 'Knowledge base']);
            await shot('support-side');
            await sidePane.getByRole('button', { name: 'Close' }).click();
            await page.waitForFunction(() => !document.querySelector('[data-scene-outlet="side"]')?.textContent?.includes('Knowledge base'));
            check('closing the pane leaves only the page active', await active('Sidebar'), ['Tickets']);
        } else {
            await page.waitForFunction(() => document.querySelector('[data-scene-outlet="primary"]')?.getAttribute('data-scene-screen') === 'ArticleScreen');
            check('a shell without the slot opens the slot item in the primary outlet', await page.locator('[data-scene-outlet="primary"]').getAttribute('data-scene-screen'), 'ArticleScreen');
            check('a shell without the slot draws no side pane', await page.locator('[data-scene-outlet="side"]').count(), 0);
        }

        await goTo('Modules', 'Home', '#/Welcome');
        check('leaving the module restores the application sidebar', await items(page, 'Sidebar'), ['Overview']);
        check('no error was raised in the page', errors, []);

        result(0);
    } catch (error) {
        failures.push(`the scenario stopped: ${error.message}`);
        result(2, error.stack);
    } finally {
        if (browser) await browser.close();
        process.exit(exitCode);
    }

    function result(code, stack) {
        exitCode = failures.length > 0 ? (code === 2 ? 2 : 1) : code;
        fs.writeFileSync(resultPath, `${JSON.stringify({ name: `shell-${mode}`, mode, passed, failures, ...(stack ? { stack } : {}) }, null, 2)}\n`);
        console.log(`${passed.length} assertions passed, ${failures.length} failed`);
        failures.forEach(failure => console.log(`FAIL ${failure}`));
    }
})();
