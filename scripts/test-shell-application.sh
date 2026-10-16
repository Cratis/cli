#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Proves the application shell constructs - navigation regions that inherit, extend and override across modules, a toolbar button that
# invokes a State Change slice as a task dialog, deep links and sample data that never ships - through a packed CLI, twice:
#
#   1. `cratis run` serves Integration/Cli/Shell/model on a live Stage (the cratis/stage image the CLI bundles) and the browser scenario
#      drives it.
#   2. `cratis render` generates the application, it is built and launched on its own backend, and the same browser scenario drives it,
#      using the generated-React harness of the Stage release the CLI bundles.
#
# It also checks that `cratis run` reports a navigation, toolbar and sample data error with its code before it starts Docker.
#
# usage: test-shell-application.sh <nupkg-feed> <package-version>
# exit:  0 every assertion passed, 1 an assertion failed, 2 the check could not run.
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
fixture="$repo_root/Integration/Cli/Shell"

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

work_root=$(mktemp -d "${TMPDIR:-/tmp}/cratis-cli-shell.XXXXXX")
keep=${CRATIS_SHELL_KEEP:-}
run_pid=""
cleanup() {
    if [[ -n "$run_pid" ]] && kill -0 "$run_pid" 2>/dev/null; then
        kill -TERM "$run_pid" 2>/dev/null
        for _ in $(seq 1 20); do kill -0 "$run_pid" 2>/dev/null || break; sleep 1; done
        kill -KILL "$run_pid" 2>/dev/null
    fi
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

echo "== Installing Cratis.Cli $version (bundles Stage $stage_version)"
dotnet tool install Cratis.Cli --tool-path "$work_root/tool" --version "$version" --configfile "$tool_config" >/dev/null \
    || could_not_run "the packed CLI did not install"
cratis="$work_root/tool/cratis"

verification="$work_root/verification"
harness="$verification/generated-react-browser"
echo "== Fetching Stage $stage_version generated-React harness (playwright, run.sh)"
mkdir -p "$verification"
curl --fail --silent --show-error --location "https://codeload.github.com/Cratis/Stage/tar.gz/refs/tags/v$stage_version" \
    | tar -xz -C "$verification" --strip-components=2 "Stage-$stage_version/Verification" \
    || could_not_run "Stage v$stage_version has no Verification folder"
(
    cd "$harness" || exit 2
    npm ci --no-audit --no-fund || exit 2
    if [[ -n "${CI:-}" ]]; then npx playwright install --with-deps chromium; else npx playwright install chromium; fi || exit 2
) >"$work_root/harness-install.log" 2>&1 || could_not_run "the browser harness could not be installed; see $work_root/harness-install.log"

failed=0

echo "== cratis run refuses a broken shell before Docker starts"
broken="$work_root/broken"
mkdir -p "$broken"
python3 - "$fixture/model/application.play" "$broken/application.play" <<'PY' || could_not_run "the broken shell could not be written"
import sys
text = open(sys.argv[1]).read()
text = text.replace('target Sales.Orders\n', 'target Sales.Nowhere\n').replace('slice Sales.Orders.PlaceOrder', 'slice Sales.Orders.OrderList').replace('active Demo', 'active Missing')
open(sys.argv[2], 'w').write(text)
PY
"$cratis" run "$broken" --port 19631 -o plain >"$work_root/run-broken.out" 2>"$work_root/run-broken.err"
broken_exit=$?
for code in PLAY0714 PLAY0748 PLAY0762; do
    grep -q "$code" "$work_root/run-broken.err" "$work_root/run-broken.out" || { echo "FAIL cratis run did not report $code"; failed=1; }
done
[[ $broken_exit -eq 5 ]] || { echo "FAIL cratis run exited $broken_exit for a broken shell, expected 5"; failed=1; }
[[ -z "$(docker ps --quiet --filter publish=19631)" ]] || { echo "FAIL cratis run started a container for a broken shell"; failed=1; }

echo "== cratis run serves the shell on a live Stage"
live_port=${CRATIS_SHELL_PORT:-19641}
"$cratis" run "$fixture/model" --port "$live_port" --workbench-port $((live_port + 1)) -o plain >"$work_root/run-live.out" 2>&1 &
run_pid=$!
ready=0
for _ in $(seq 1 120); do
    if curl --silent --fail --output /dev/null "http://127.0.0.1:$live_port/stage/status"; then ready=1; break; fi
    kill -0 "$run_pid" 2>/dev/null || break
    sleep 2
done
[[ $ready -eq 1 ]] || { tail -30 "$work_root/run-live.out" >&2; could_not_run "cratis run did not start a Stage; see output above"; }

mkdir -p "$work_root/results"
SHELL_URL="http://127.0.0.1:$live_port" SHELL_MODE=live SHELL_RESULT="$work_root/results/live.json" SHELL_SCREENSHOTS="$work_root/results" \
NODE_PATH="$harness/node_modules${NODE_PATH:+:$NODE_PATH}" \
    node "$fixture/shell-navigation.cjs"
live_exit=$?
[[ $live_exit -eq 2 ]] && could_not_run "the live scenario could not run"
[[ $live_exit -eq 0 ]] || failed=1

kill -TERM "$run_pid" 2>/dev/null
for _ in $(seq 1 30); do kill -0 "$run_pid" 2>/dev/null || break; sleep 1; done
run_pid=""
[[ -z "$(docker ps --quiet --filter publish=$live_port)" ]] || { echo "FAIL cratis run left its container running"; failed=1; }

echo "== cratis render generates the shell application"
# Stage's generated-React harness launches Workspaces.csproj, so the application takes that name.
application="$work_root/Workspaces"
"$cratis" render "$fixture/model" --name Workspaces --destination "$application" -o json-compact >"$work_root/render.json" \
    || could_not_run "cratis render refused the shell; see $work_root/render.json"
[[ -f "$application/Workspaces.csproj" && -f "$application/package.json" ]] || could_not_run "the rendered application has no Workspaces.csproj or package.json"

echo "== Building the generated backend and frontend"
(
    cd "$application" || exit 2
    git init --quiet || exit 2
    dotnet restore Workspaces.csproj || exit 2
    dotnet build Workspaces.csproj --no-restore --configuration Debug || exit 2
    dotnet build Workspaces.csproj --no-restore --configuration Release || exit 2
    npm install --no-audit --no-fund || exit 2
    npm run build || exit 2
) >"$work_root/build.log" 2>&1 || { tail -40 "$work_root/build.log" >&2; could_not_run "the generated application did not build"; }
[[ -f "$application/wwwroot/index.html" ]] || could_not_run "the frontend build produced no wwwroot/index.html"

echo "== Launching the rendered application and running the same scenario"
STAGE_GENERATED_REACT_SCENARIO="$fixture/shell-navigation.cjs" bash "$harness/run.sh" "$application" "$work_root/results/generated.json"
generated_exit=$?
[[ $generated_exit -eq 2 ]] && could_not_run "the generated scenario could not run"
[[ $generated_exit -eq 0 ]] || failed=1

if [[ -n "${CRATIS_SHELL_RESULTS:-}" ]]; then
    mkdir -p "$CRATIS_SHELL_RESULTS"
    cp -R "$work_root/results/." "$CRATIS_SHELL_RESULTS/"
fi

python3 - "$work_root/results" <<'PY'
import json, sys, pathlib
for name in ('live', 'generated'):
    result = json.load(open(pathlib.Path(sys.argv[1]) / f'{name}.json'))
    print(f"{name}: {len(result['passed'])} passed, {len(result['failures'])} failed")
PY
exit $failed
