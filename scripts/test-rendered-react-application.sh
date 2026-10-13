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
# It then approves a custom runtime package (Integration/Cli/RenderedReact/CustomPackage/acme-scene-charts) with
# `cratis render --package-set`, renders the corpus again with a screen that names a component of that package, rebuilds the
# frontend, relaunches the application and asserts in a browser that the application loaded the package and rendered its
# component - the custom package set working without Studio, from the same packed CLI. Finally it runs the same package set live with
# `cratis run --package-set` (the cratis/stage image loads the package at runtime) and runs the same browser scenario against it.
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
# Stage's harness reaches sibling verification scripts (../verify-*.py), so keep its Verification layout.
verification="$work_root/verification"
harness="$verification/generated-react-browser"
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
mkdir -p "$verification"
curl --fail --silent --show-error --location "https://codeload.github.com/Cratis/Stage/tar.gz/refs/tags/v$stage_version" \
    | tar -xz -C "$verification" --strip-components=2 "Stage-$stage_version/Verification" \
    || could_not_run "Stage v$stage_version has no Verification folder"
[[ -f "$harness/run.sh" ]] || could_not_run "Stage v$stage_version has no Verification/generated-react-browser"
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
plain_exit=$?

echo "== Approving a custom runtime package and rendering again"
custom_dir="$repo_root/Integration/Cli/RenderedReact/CustomPackage"
package_dir="$work_root/acme-scene-charts"
cp -R "$custom_dir/acme-scene-charts" "$package_dir"
(cd "$package_dir" && npm pack --silent >/dev/null) || could_not_run "the custom package could not be packed"
package_tgz=$(ls "$package_dir"/*.tgz | head -n 1)
package_set="$work_root/package-set.json"
cat >"$package_set" <<JSON
{
  "version": 1,
  "packages": [
    {
      "manifest": { "name": "Acme.Charts", "version": "1.2.0", "kind": "ComponentLibrary", "components": ["chart"] },
      "module": { "npm": "@acme/scene.charts", "version": "file:$package_tgz", "export": "acmeChartsRuntimePackage", "styles": ["@acme/scene.charts/styles.css"] }
    }
  ]
}
JSON
python3 - "$source_dir" <<'PY' || could_not_run "the corpus could not be extended with the custom package"
import sys, pathlib
root = pathlib.Path(sys.argv[1])
application = root / 'application.play'
text = application.read_text()
assert '    Cratis.Components\n' in text
application.write_text(text.replace('    Cratis.Components\n', '    Cratis.Components\n    Acme.Charts\n', 1))
screen = next(root.rglob('WorkItemList.play'))
text = screen.read_text()
marker = '          details\n'
assert marker in text
screen.write_text(text.replace(marker, marker + '            component Acme.Charts.chart salesChart\n              property title = "Sales by week"\n', 1))
PY

"$work_root/tool/cratis" render "$source_dir" --name Workspaces --destination "$application" --package-set "$package_set" -o json-compact >"$work_root/render-custom.json" \
    || could_not_run "cratis render refused the corpus with the approved package set; see $work_root/render-custom.json"
[[ -f "$application/approved-packages.json" && -f "$application/.frontend/approvedPackages.ts" ]] \
    || could_not_run "the rendered application has no approved package set or loader"

(
    cd "$application" || exit 2
    npm install --no-audit --no-fund || exit 2
    npm run build || exit 2
) || could_not_run "the frontend with the approved package did not build"

custom_result="$work_root/custom-package-browser-result.json"
STAGE_GENERATED_REACT_SCENARIO="$custom_dir/custom-package.cjs" bash "$harness/run.sh" "$application" "$custom_result"
custom_exit=$?
[[ $custom_exit -eq 2 ]] && could_not_run "the custom package scenario could not run"
if [[ $custom_exit -ne 0 ]]; then
    echo "custom package scenario failed; see $custom_result"
fi

echo "== Running the same package set live with cratis run"
# `cratis run` resolves the profile against the shipped and approved packages before Docker starts: without the set the corpus
# (which now names Acme.Charts) is refused with CLI-RENDER-007 and nothing is started.
refused="$work_root/run-refused.json"
"$work_root/tool/cratis" run "$source_dir" --output json-compact >"$refused" 2>&1
refused_exit=$?
if [[ $refused_exit -ne 5 ]] || ! grep -q 'CLI-RENDER-007' "$refused"; then
    echo "cratis run did not refuse the unapproved package before starting Docker (exit $refused_exit); see $refused"
    live_exit=1
else
    echo "cratis run refused the unapproved package with CLI-RENDER-007"
    live_packages="$work_root/live-packages"
    mkdir -p "$live_packages/@acme"
    cp -R "$custom_dir/acme-scene-charts" "$live_packages/@acme/scene.charts"
    sed "s#file:$package_tgz#1.2.0#" "$package_set" >"$live_packages/approved-packages.json"

    live_log="$work_root/run-live.json"
    live_port=${CRATIS_RENDERED_REACT_LIVE_PORT:-19391}
    "$work_root/tool/cratis" run "$source_dir" --package-set "$live_packages/approved-packages.json" --port "$live_port" --workbench-port "$((live_port + 1))" --output json-compact >"$live_log" 2>&1 &
    run_pid=$!
    live_ready=""
    for _ in $(seq 1 120); do
        grep -q '"status":"ready"' "$live_log" 2>/dev/null && { live_ready=yes; break; }
        kill -0 "$run_pid" 2>/dev/null || break
        sleep 2
    done

    if [[ -z "$live_ready" ]]; then
        echo "cratis run did not become ready with the approved package set; see $live_log"
        cat "$live_log" | tail -20
        live_exit=1
    else
        live_result="$work_root/live-custom-package-browser-result.json"
        NODE_PATH="$harness/node_modules" STAGE_GENERATED_REACT_URL="http://127.0.0.1:$live_port" STAGE_GENERATED_REACT_RESULT="$live_result" \
            node "$custom_dir/custom-package.cjs"
        live_exit=$?
        [[ $live_exit -eq 2 ]] && could_not_run "the custom package scenario could not run against cratis run"
        [[ $live_exit -ne 0 ]] && echo "live custom package scenario failed; see $live_result"
    fi

    # A job started with & in a non-interactive shell ignores SIGINT; SIGTERM is the plain kill `cratis run` also stops the
    # container for. If it has not exited in time, stop what it started rather than hang the check.
    kill -TERM "$run_pid" 2>/dev/null
    for _ in $(seq 1 60); do
        kill -0 "$run_pid" 2>/dev/null || break
        sleep 1
    done
    if kill -0 "$run_pid" 2>/dev/null; then
        echo "cratis run did not stop on SIGTERM; stopping its container"
        kill -KILL "$run_pid" 2>/dev/null
    fi
    for container in $(docker ps --quiet --filter "publish=$live_port"); do
        docker rm --force "$container" >/dev/null 2>&1
    done
    wait "$run_pid" 2>/dev/null
fi

[[ $plain_exit -ne 0 || $custom_exit -ne 0 || ${live_exit:-0} -ne 0 ]] && exit 1
exit 0
