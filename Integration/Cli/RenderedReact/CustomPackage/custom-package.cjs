// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Browser scenario for a rendered application that was approved to render with a custom runtime package
// (Acme.Charts, acme-scene-charts). It runs inside Stage's generated-React harness (STAGE_GENERATED_REACT_SCENARIO), which
// serves the rendered application on its generated backend and provides playwright. It asserts the application loaded the
// approved package at runtime and rendered the authored `component Acme.Charts.chart` from it, and that the shipped
// Cratis components still render beside it.
//
// Exit codes: 0 every assertion passed, 1 an assertion found a defect, 2 the scenario could not run.

const { writeFileSync } = require('node:fs');
const { chromium } = require('playwright');

const baseUrl = process.env.STAGE_GENERATED_REACT_URL;
const resultPath = process.env.STAGE_GENERATED_REACT_RESULT ?? 'custom-package-result.json';

(async () => {
    const result = { name: 'custom-package-browser', baseUrl, status: 'unknown', remainingBlockers: [], assertions: [] };
    const record = (path, status, value) => result.assertions.push({ path, status, value });
    const block = (path, reason) => { record(path, 'blocked', reason); result.remainingBlockers.push(`${path}: ${reason}`); };
    let exitCode = 2;
    let browser;
    try {
        browser = await chromium.launch();
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });

        const chart = page.locator('[data-acme-chart="salesChart"]');
        try {
            await chart.waitFor({ timeout: 20_000 });
            record('browser.customPackage.rendered', 'passed', 'the authored Acme.Charts.chart rendered from the approved package');
        } catch {
            block('browser.customPackage.rendered', `no [data-acme-chart="salesChart"] element; page errors: ${errors.join(' | ') || 'none'}`);
        }

        if (await chart.count() > 0) {
            const text = (await chart.first().innerText()).trim();
            text === 'Sales by week'
                ? record('browser.customPackage.property', 'passed', text)
                : block('browser.customPackage.property', `expected the authored title 'Sales by week', got '${text}'`);

            const border = await chart.first().evaluate(element => getComputedStyle(element).borderTopStyle);
            border === 'solid'
                ? record('browser.customPackage.styles', 'passed', 'the package style sheet was applied')
                : block('browser.customPackage.styles', `expected the package style sheet to give the chart a solid border, got '${border}'`);
        }

        try {
            await page.getByText('Work items', { exact: false }).first().waitFor({ timeout: 10_000 });
            record('browser.customPackage.shippedComponents', 'passed', 'the shipped Cratis components render beside the custom package');
        } catch {
            block('browser.customPackage.shippedComponents', 'the shipped Cratis components did not render');
        }

        const alert = await page.locator('#root[role="alert"]').count();
        alert === 0
            ? record('browser.customPackage.noLoadDiagnostics', 'passed', 'the application reported no package load diagnostics')
            : block('browser.customPackage.noLoadDiagnostics', await page.locator('#root').innerText());

        const passed = result.assertions.filter(assertion => assertion.status === 'passed').length;
        result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
        result.summary = `${passed} assertions passed, ${result.remainingBlockers.length} blocked`;
        exitCode = result.remainingBlockers.length > 0 ? 1 : 0;
    } catch (error) {
        result.status = 'could-not-run';
        result.message = error.stack ?? error.message;
    } finally {
        if (browser) await browser.close();
        writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    }

    console.log(result.summary ?? result.message);
    for (const blocker of result.remainingBlockers) console.log(`BLOCKED ${blocker}`);
    process.exit(exitCode);
})();
