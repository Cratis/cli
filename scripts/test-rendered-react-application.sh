#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Renders the canonical screen-composition corpus with a packed CLI, builds the generated application - backend and
# React frontend - launches it on its own generated backend against a pinned Chronicle container, and runs Stage's
# generated-React browser scenarios against it.
#
# The scenarios are Stage's own (Verification/generated-react-browser), taken from the Stage release this CLI bundles,
# so the rendered application is held to the same assertions the live Stage runtime is, at exactly the renderer
# version that produced it. Nothing is copied into this repository.
#
# usage: test-rendered-react-application.sh <nupkg-feed> <package-version>
# exit:  0 every scenario passed, 1 a scenario found a defect, 2 the check could not run.
set -uo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <nupkg-feed> <package-version>" >&2
    exit 2
fi

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repo_root=$(cd -- "$script_dir/.." && pwd -P)
feed=$1
version=$2
[[ "$feed" == /* ]] || feed=$(cd -- "$(dirname -- "$feed")" && pwd -P)/$(basename -- "$feed")

could_not_run() {
    echo "could not run: $1" >&2
    exit 2
}

[[ -f "$feed/Cratis.Cli.$version.nupkg" ]] || could_not_run "the packed CLI was not found at $feed/Cratis.Cli.$version.nupkg"
for tool in dotnet node npm npx docker curl tar python3; do
    command -v "$tool" >/dev/null || could_not_run "'$tool' is not on PATH"
done

stage_version=$(grep -oE '<StageVersion[^>]*>[0-9]+\.[0-9]+\.[0-9]+</StageVersion>' "$repo_root/Directory.Packages.props" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')
[[ -n "$stage_version" ]] || could_not_run "no StageVersion in Directory.Packages.props"

work_root=$(mktemp -d "${TMPDIR:-/tmp}/cratis-cli-rendered-react.XXXXXX")
keep=${CRATIS_RENDERED_REACT_KEEP:-}
cleanup() {
    if [[ -n "$keep" ]]; then
        echo "kept $work_root"
    else
        rm -rf "$work_root"
    fi
}
trap cleanup EXIT

export CRATIS_NO_UPDATE_CHECK=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 NO_COLOR=1 TERM=dumb

tool_config="$work_root/tool-nuget.config"
cat >"$tool_config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="sentinel" value="$feed" />
  </packageSources>
</configuration>
EOF

source_dir="$work_root/source"
application="$work_root/Workspaces"
harness="$work_root/harness"
result="$work_root/generated-react-browser-result.json"

echo "== Installing Cratis.Cli $version (bundles Stage $stage_version)"
dotnet tool install Cratis.Cli --tool-path "$work_root/tool" --version "$version" --configfile "$tool_config" >/dev/null \
    || could_not_run "the packed CLI did not install"

echo "== Writing the canonical screen-composition corpus"
dotnet run --project "$repo_root/Integration/Cli/RenderedReact/CorpusSource/CorpusSource.csproj" --configuration Release -- "$source_dir" \
    || could_not_run "the corpus could not be written"

echo "== Rendering"
"$work_root/tool/cratis" render "$source_dir" --name Workspaces --destination "$application" -o json-compact >"$work_root/render.json" \
    || could_not_run "cratis render refused the corpus; see $work_root/render.json"
python3 - "$work_root/render.json" <<'PY' || could_not_run "cratis render published no artifacts"
import json, sys
for line in open(sys.argv[1]):
    line = line.strip()
    if line.startswith('{') and 'artifacts' in json.loads(line):
        artifacts = json.loads(line)['artifacts']
        print(f"rendered {artifacts} artifacts")
        sys.exit(0 if artifacts > 0 else 1)
sys.exit(1)
PY
[[ -f "$application/Workspaces.csproj" && -f "$application/package.json" ]] || could_not_run "the rendered application has no Workspaces.csproj or package.json"

echo "== Building the generated backend (Debug writes the proxies the frontend imports)"
(
    cd "$application" || exit 2
    git init --quiet || exit 2
    dotnet restore Workspaces.csproj || exit 2
    dotnet build Workspaces.csproj --no-restore --configuration Debug || exit 2
    dotnet build Workspaces.csproj --no-restore --configuration Release || exit 2
) || could_not_run "the generated backend did not build"

echo "== Building the generated React frontend"
(
    cd "$application" || exit 2
    npm install --no-audit --no-fund || exit 2
    npm run build || exit 2
) || could_not_run "the generated frontend did not build"
[[ -f "$application/wwwroot/index.html" ]] || could_not_run "the frontend build produced no wwwroot/index.html"

echo "== Fetching Stage $stage_version generated-React scenarios"
mkdir -p "$harness"
curl --fail --silent --show-error --location "https://codeload.github.com/Cratis/Stage/tar.gz/refs/tags/v$stage_version" \
    | tar -xz -C "$harness" --strip-components=3 "Stage-$stage_version/Verification/generated-react-browser" \
    || could_not_run "Stage v$stage_version has no Verification/generated-react-browser"
(
    cd "$harness" || exit 2
    npm ci --no-audit --no-fund || exit 2
    if [[ -n "${CI:-}" ]]; then npx playwright install --with-deps chromium; else npx playwright install chromium; fi || exit 2
) || could_not_run "the Stage scenarios could not be installed"

echo "== Launching the rendered application and running the scenarios"
bash "$harness/run.sh" "$application" "$result"
scenario_exit=$?
[[ $scenario_exit -eq 2 ]] && could_not_run "the Stage scenarios could not run against the rendered application"

python3 - "$result" "$scenario_exit" <<'PY'
import json, sys
result_path, scenario_exit = sys.argv[1], int(sys.argv[2])
try:
    result = json.load(open(result_path))
except (OSError, ValueError) as error:
    print(f"could not run: no scenario result ({error})", file=sys.stderr)
    sys.exit(2)
passed = sum(1 for a in result.get('assertions', []) if a.get('status') == 'passed')
failed = [a for a in result.get('assertions', []) if a.get('status') not in ('passed', 'info')]
blockers = result.get('remainingBlockers', [])
print(f"scenario status {result.get('status')}: {passed} passed, {len(failed)} not passed, {len(blockers)} blockers")
for blocker in blockers:
    print(f"  {blocker}")
if passed == 0:
    print("could not run: the scenarios asserted nothing", file=sys.stderr)
    sys.exit(2)
sys.exit(0 if scenario_exit == 0 and result.get('status') == 'passed' and not failed and not blockers else 1)
PY
