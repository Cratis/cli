#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)

assert_required() {
    local name=$1 expected=$2 paths=$3 actual
    actual=$(printf '%s' "$paths" | bash "$script_dir/render-legs-required.sh")
    if [[ "$actual" != "$expected" ]]; then
        printf 'FAIL %s: expected %s, got %s\n' "$name" "$expected" "$actual" >&2
        exit 1
    fi
    printf 'PASS %s\n' "$name"
}

assert_required 'empty input' true ''
assert_required 'docs only' false $'Documentation/guide.txt\nREADME.md\nSource/Cli/README.md\n'
assert_required 'specs only' false $'Source/Cli.Specs/example.cs\nSource/Templating.Specs/example.cs\nSource/Templating.Conformance/example.cs\n'
assert_required 'Chronicle integration' false $'Integration/Chronicle/example.cs\n'
for folder in Arc Completions Context Direct Llm Prologue ReleaseNotes View; do
    assert_required "safe command: $folder" false "Source/Cli/Commands/$folder/example.cs"
done
assert_required 'mixed safe and Render' true $'Documentation/guide.md\nSource/Cli/Commands/Render/RenderCommand.cs\n'
assert_required 'package versions' true $'Directory.Packages.props\n'
assert_required 'props under a safe folder' true $'Source/Cli.Specs/Build.props\n'
assert_required 'targets under documentation' true $'Documentation/Build.targets\n'
assert_required 'shell script' true $'scripts/test-shell-application.sh\n'
assert_required 'workflow' true $'.github/workflows/build.yml\n'
assert_required 'CLI root' true $'Source/Cli/Program.cs\n'
assert_required 'CLI integration' true $'Integration/Cli/Shell/x\n'
assert_required 'CLI integration markdown fixture' true $'Integration/Cli/Shell/model/README.md\n'
assert_required 'script markdown' true $'scripts/README.md\n'
assert_required 'rename out of a required folder' true $'Integration/Cli/Shell/shell-navigation.cjs\nSource/Cli.Specs/Shell/shell-navigation.cjs\n'
assert_required 'SDK version' true $'global.json\n'
assert_required 'unknown path' true $'unknown/file\n'
assert_required 'blank path' true $'\n'
assert_required 'safe then unknown' true $'Source/Cli/Commands/Arc/example.cs\nunknown/file\n'
for folder in Ai Chronicle LlmContext Version Render Run Screenplay Init New GetStarted; do
    assert_required "required command: $folder" true "Source/Cli/Commands/$folder/example.cs"
done
for folder in Registration Templating; do
    assert_required "shared CLI code: $folder" true "Source/Cli/$folder/example.cs"
done
assert_required 'template source' true $'Source/Templating/example.cs\n'
assert_required 'CLI generator' true $'Source/Cli.Generators/example.cs\n'
