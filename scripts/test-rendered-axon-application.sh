#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Usage: scripts/test-rendered-axon-application.sh <local-package-feed> <version>
# Requires JDK 25, node, and the .NET SDK. Exit 0 = proved, 1 = failed assertion,
# 2 = could not run (including an unavailable released Axon facade). Work stays in .ai-work/.
# Uses the checked-in ingress-invariants.play (Model A) and the linked React corpus
# source (Model B) under Integration/Cli/RenderedAxon. No sibling checkout is used.
set -uo pipefail

could_not_run() { echo "could not run: $*" >&2; exit 2; }
failed() { echo "failed: $*" >&2; exit 1; }
[[ $# -eq 2 ]] || could_not_run "usage: $0 <local-package-feed> <version>"
feed=$1
version=$2
for command in dotnet java node; do
    command -v "$command" >/dev/null 2>&1 || could_not_run "$command is required"
done
java_version=$(java -version 2>&1) || could_not_run "Java is unavailable"
[[ "$java_version" == *'version "25.'* || "$java_version" == *'version "25"'* ]] || could_not_run "JDK 25 is required"
repo_root=$(cd "$(dirname "$0")/.." && pwd) || could_not_run "cannot locate repository"
mkdir -p "$repo_root/.ai-work" || could_not_run "cannot create .ai-work"
work_root=$(mktemp -d "$repo_root/.ai-work/rendered-axon.XXXXXX") || could_not_run "cannot create work directory"
echo "== Artifacts: $work_root"
dotnet tool install Cratis.Cli --version "$version" --tool-path "$work_root/tool" --add-source "$feed" \
    || could_not_run "the packed CLI could not be installed"
cli="$work_root/tool/cratis"

# Probe the actual shipped CLI first, rather than a separate Stage verification executable.
cat >"$work_root/probe.play" <<'PLAY'
concept ProbeId : Uuid
module Probe
  feature Submission
    slice StateChange Submit
      command Submit
        probeId ProbeId identifier
        produces Submitted
          for probeId
          probeId = probeId
      event Submitted
        probeId ProbeId
PLAY
"$cli" render "$work_root/probe.play" --target axon --name Probe --destination "$work_root/probe" -o json-compact \
    >"$work_root/probe.json" 2>"$work_root/probe.stderr"
probe_exit=$?
[[ $probe_exit -eq 0 ]] || could_not_run "the Axon target probe failed (exit $probe_exit); see $work_root/probe.stderr"

model="$work_root/ingress-invariants.play"
cp "$repo_root/Integration/Cli/RenderedAxon/ingress-invariants.play" "$model" \
    || could_not_run "the ingress-invariants.play fixture is unavailable"
[[ -s "$model" ]] || could_not_run "the ingress fixture is empty"
application="$work_root/a"
"$cli" render "$model" --target axon --name IngressInvariants --destination "$application" -o json-compact \
    >"$work_root/a.json" 2>"$work_root/a.stderr" || could_not_run "Axon refused Model A; see $work_root/a.stderr"
node --input-type=module - "$work_root/a.stderr" <<'JS'
import fs from 'node:fs';
const text = fs.readFileSync(process.argv[2], 'utf8');
if ((text.match(/CLI-RENDER-029/g) ?? []).length !== 1) throw new Error('Expected exactly one experimental diagnostic');
JS
[[ $? -eq 0 ]] || failed "the experimental diagnostic was missing or repeated"
[[ -f "$application/mvnw" && -f "$application/pom.xml" ]] || could_not_run "the generated Maven application is absent"
(cd "$application" && sh ./mvnw -B verify) || failed "Model A Maven verification failed"
node --input-type=module - "$application" <<'JS'
import fs from 'node:fs';
import path from 'node:path';
const reports = [...fs.globSync('**/target/surefire-reports/TEST-*.xml', { cwd: process.argv[2] })];
let count = 0;
for (const report of reports) {
    const xml = fs.readFileSync(path.join(process.argv[2], report), 'utf8');
    const suite = xml.match(/<testsuite\b[^>]*>/)?.[0] ?? '';
    const attribute = name => Number(suite.match(new RegExp(`\\b${name}="([0-9]+)"`))?.[1] ?? 0);
    if (!suite || attribute('failures') || attribute('errors')) throw new Error(`Failed or invalid Maven report: ${report}`);
    count += attribute('tests') - attribute('skipped');
}
if (count <= 0) throw new Error('No executed Model A tests');
console.log(`Model A: ${count} executed tests in ${reports.length} reports`);
JS
[[ $? -eq 0 ]] || failed "Model A executed no successful native tests"
"$cli" render "$model" --target axon --name IngressInvariants --destination "$application" --check -o json-compact \
    >"$work_root/a-check.json" 2>"$work_root/a-check.stderr"
[[ $? -eq 0 ]] || failed "unchanged Model A did not pass --check"
node --input-type=module - "$model" <<'JS'
import fs from 'node:fs';
const file = process.argv[2];
const source = fs.readFileSync(file, 'utf8');
const fields = 'readmodel ActiveAccounts\n        id AccountId\n        alias String\n        actor String';
if (!source.includes(fields)) throw new Error('Expected ingress ActiveAccounts fields');
fs.writeFileSync(file, source.replace(fields, `${fields}\n        note String`));
JS
[[ $? -eq 0 ]] || could_not_run "the ingress fixture could not be mutated"
"$cli" render "$model" --target axon --name IngressInvariants --destination "$application" --check -o json-compact \
    >"$work_root/a-drift.json" 2>"$work_root/a-drift.stderr"
drift_exit=$?
# Exit 6 is ChangesPending: compiler refusals are not evidence of drift.
[[ $drift_exit -eq 6 ]] || failed "changed Model A did not report drift (exit $drift_exit)"

source_dir="$work_root/react-source"
dotnet run --project "$repo_root/Integration/Cli/RenderedAxon/CorpusSource/CorpusSource.csproj" --configuration Release -- "$source_dir" \
    || could_not_run "the CLI React corpus could not be exported"
"$cli" render "$source_dir" --target axon --name Workspaces --destination "$work_root/b" -o json-compact \
    >"$work_root/b.json" 2>"$work_root/b.stderr" || failed "Model B did not render; see $work_root/b.stderr"
# Since Axon 0.8.0 Model B renders without unsupported constructs, so STAGE-AXON warnings are reported, not required.
[[ -f "$work_root/b/UNSUPPORTED.md" ]] || failed "Model B has no UNSUPPORTED.md report"
echo "Model B STAGE-AXON warnings: $(grep -c 'STAGE-AXON-' "$work_root/b.stderr")"
[[ -f "$work_root/b/mvnw" ]] || could_not_run "Model B has no Maven wrapper"
(cd "$work_root/b" && sh ./mvnw -B -q compile) >"$work_root/b-compile.log" 2>&1
model_b_exit=$?
echo "Model B Maven compile exit: $model_b_exit (reported, not a gate); see $work_root/b-compile.log"
echo "Model A render, native tests, no-drift and drift checks passed; Model B rendered."
exit 0
