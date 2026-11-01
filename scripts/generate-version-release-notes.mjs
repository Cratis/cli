// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Writes the release notes the CLI embeds and shows through 'cratis release-notes' and after 'cratis update'.
 *
 * For version X it writes `<output>/X.md`. Source/Cli/Cli.csproj embeds every file in the folder.
 *
 * Usage:
 *   One version, from the merged pull request description (the publish workflow):
 *     node scripts/generate-version-release-notes.mjs --version <version> --body-file <path> --output <dir>
 *   Every published release, from the GitHub releases API (a one-off backfill):
 *     gh api --paginate 'repos/Cratis/cli/releases?per_page=100' > releases.json
 *     node scripts/generate-version-release-notes.mjs --releases-file releases.json --output <dir>
 */

import { promises as fs } from 'node:fs';
import * as path from 'node:path';

const versionPattern = /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/;

/**
 * Remove content that never ships in user-facing notes: HTML comments (the pull request template guidance)
 * and any Copilot "Original prompt" transcript block.
 * @param {string} body - The raw description.
 * @returns {string} The cleaned markdown.
 */
export function cleanBody(body) {
    return body
        .replace(/\r\n/g, '\n')
        .replace(/<!--[\s\S]*?-->/g, '')
        .replace(/#{1,6}\s*Original prompt[\s\S]*?(?=\n#{1,6}\s|\n?$)/gi, '')
        .replace(/\n{3,}/g, '\n\n')
        .trim();
}

/**
 * Normalize a tag or version (`v1.2.3` or `1.2.3`) to the file name version, refusing anything else.
 * @param {string} value - The tag or version.
 * @returns {string} The version without a leading `v`.
 */
export function normalizeVersion(value) {
    const version = value.trim().replace(/^v/, '');
    if (!versionPattern.test(version)) {
        throw new Error(`Not a release version: '${value}'`);
    }
    return version;
}

async function writeNote(output, version, body) {
    const notes = cleanBody(body ?? '');
    await fs.writeFile(path.join(output, `${version}.md`), `${notes.length > 0 ? notes : 'No release notes.'}\n`);
}

/**
 * Parse `--name value` pairs.
 * @param {string[]} argv - The arguments after the script name.
 * @returns {Record<string, string>} The options.
 */
function parseArguments(argv) {
    const options = {};
    for (let index = 0; index < argv.length; index += 2) {
        if (!argv[index].startsWith('--') || argv[index + 1] === undefined) {
            throw new Error(`Unexpected argument '${argv[index]}'`);
        }
        options[argv[index].slice(2)] = argv[index + 1];
    }
    return options;
}

async function main() {
    const options = parseArguments(process.argv.slice(2));
    if (!options.output) {
        throw new Error('--output is required');
    }
    await fs.mkdir(options.output, { recursive: true });

    if (options['releases-file']) {
        // `gh api --paginate` writes one JSON array per page, back to back.
        const raw = await fs.readFile(options['releases-file'], 'utf8');
        const releases = JSON.parse(`[${raw.replace(/\]\s*\[/g, '],[')}]`).flat().filter(_ => !_.draft);
        if (releases.length === 0) {
            throw new Error('The releases file holds no published releases');
        }
        for (const release of releases) {
            await writeNote(options.output, normalizeVersion(release.tag_name), release.body);
        }
        console.log(`Wrote release notes for ${releases.length} releases to ${options.output}`);
        return;
    }

    if (!options.version || !options['body-file']) {
        throw new Error('Either --releases-file, or --version and --body-file, are required');
    }
    const version = normalizeVersion(options.version);
    await writeNote(options.output, version, await fs.readFile(options['body-file'], 'utf8'));
    console.log(`Wrote release notes for ${version} to ${options.output}`);
}

main().catch(error => {
    console.error(error.message);
    process.exit(1);
});
