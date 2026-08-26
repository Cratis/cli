#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <nupkg-feed> <package-version>" >&2
    exit 2
fi

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
repo_root=$(cd -- "$script_dir/.." && pwd -P)
feed=$1
version=$2

if [[ "$feed" != /* ]]; then
    feed=$(cd -- "$(dirname -- "$feed")" && pwd -P)/$(basename -- "$feed")
fi

package="$feed/Cratis.Cli.$version.nupkg"
if [[ ! -f "$package" ]]; then
    echo "Packed CLI was not found at $package" >&2
    exit 2
fi

work_root=$(mktemp -d "${TMPDIR:-/tmp}/cratis-cli-screenplay-placement.XXXXXX")
work_root=$(cd -- "$work_root" && pwd -P)
fixture="$repo_root/Integration/Cli/ScreenplayPlacement"
fixture_artifacts_parent="$fixture/.artifacts"
fixture_artifacts="$fixture_artifacts_parent/$(basename -- "$work_root")"
trap 'rm -rf "$work_root" "$fixture_artifacts"; rmdir "$fixture_artifacts_parent" 2>/dev/null || true' EXIT

export DOTNET_CLI_HOME="$work_root/dotnet-home"
export NUGET_PACKAGES="$work_root/nuget-packages"
export NUGET_HTTP_CACHE_PATH="$work_root/nuget-http-cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export CRATIS_NO_UPDATE_CHECK=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export ScreenplayPlacementArtifactsRoot="$fixture_artifacts"
export NO_COLOR=1
export TERM=dumb

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH" "$work_root/tool" "$fixture_artifacts"

tool_nuget_config="$work_root/tool-nuget.config"
public_nuget_config="$work_root/public-nuget.config"
cat >"$tool_nuget_config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="sentinel" value="$feed" />
  </packageSources>
</configuration>
EOF
cat >"$public_nuget_config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

tool="$work_root/tool/cratis"
host="$fixture/Host/Host.csproj"
expected="$fixture/Expected.play"
file_output="$work_root/from-file.play"
stdout_output="$work_root/from-stdout.play"
file_summary="$work_root/from-file.stdout"
file_diagnostics="$work_root/from-file.stderr"
stdout_diagnostics="$work_root/from-stdout.stderr"
validation_summary="$work_root/validation.stdout"
validation_diagnostics="$work_root/validation.stderr"

dotnet tool install Cratis.Cli \
    --tool-path "$work_root/tool" \
    --version "$version" \
    --configfile "$tool_nuget_config"

dotnet restore "$host" --configfile "$public_nuget_config" --no-http-cache
dotnet build "$host" --no-restore --configuration Release

"$tool" screenplay generate "$host" \
    --provider critter-stack \
    --feature-root Source \
    --skip-segments 1 \
    --file "$file_output" \
    -o json-compact \
    >"$file_summary" \
    2>"$file_diagnostics"

"$tool" screenplay generate "$host" \
    --provider critter-stack \
    --feature-root Source \
    --skip-segments 1 \
    -o json-compact \
    >"$stdout_output" \
    2>"$stdout_diagnostics"

cmp "$file_output" "$stdout_output"
cmp "$file_output" "$expected"
cmp "$file_diagnostics" "$stdout_diagnostics"

"$tool" screenplay validate "$expected" -o json-compact \
    >"$validation_summary" \
    2>"$validation_diagnostics"

python3 - \
    "$repo_root" \
    "$work_root" \
    "$expected" \
    "$file_output" \
    "$stdout_output" \
    "$file_diagnostics" \
    "$stdout_diagnostics" \
    "$file_summary" \
    "$validation_summary" \
    "$validation_diagnostics" <<'PY'
import hashlib
import json
import sys
from pathlib import Path

(
    repo_root,
    work_root,
    expected_path,
    file_output_path,
    stdout_output_path,
    file_diagnostics_path,
    stdout_diagnostics_path,
    file_summary_path,
    validation_summary_path,
    validation_diagnostics_path,
) = sys.argv[1:]

expected = Path(expected_path)
file_output = Path(file_output_path)
stdout_output = Path(stdout_output_path)
file_diagnostics = Path(file_diagnostics_path)
stdout_diagnostics = Path(stdout_diagnostics_path)
file_summary = Path(file_summary_path)
validation_summary = Path(validation_summary_path)
validation_diagnostics = Path(validation_diagnostics_path)


def assert_clean_bytes(path: Path, *, forbid_physical_paths: bool) -> bytes:
    data = path.read_bytes()
    assert data, f"{path} is empty"
    assert not data.startswith(b"\xef\xbb\xbf"), f"{path} has a UTF-8 BOM"
    assert b"\x1b" not in data, f"{path} contains ANSI escape bytes"
    assert b"\r" not in data, f"{path} contains CR bytes"
    assert b"\x00" not in data, f"{path} contains NUL bytes"
    assert data.endswith(b"\n") and not data.endswith(b"\n\n"), f"{path} must end in exactly one LF"
    if forbid_physical_paths:
        for physical_root in (repo_root, work_root):
            assert physical_root.encode() not in data, f"{path} discloses physical root {physical_root}"
    return data


expected_bytes = assert_clean_bytes(expected, forbid_physical_paths=True)
assert_clean_bytes(file_output, forbid_physical_paths=True)
assert_clean_bytes(stdout_output, forbid_physical_paths=True)
file_diagnostics_bytes = assert_clean_bytes(file_diagnostics, forbid_physical_paths=True)
assert_clean_bytes(stdout_diagnostics, forbid_physical_paths=True)
assert_clean_bytes(file_summary, forbid_physical_paths=False)
assert_clean_bytes(validation_summary, forbid_physical_paths=False)
assert validation_diagnostics.read_bytes() == b"", "successful validation must not emit diagnostics"

source = expected_bytes.decode("utf-8")
for declaration in (
    "slice StateView Current",
    "readmodel Order",
    "reducer OrderProjection => Order",
    "slice StateChange Submit",
    "command SubmitOrder",
    "event OrderSubmitted",
    "slice StateView Summary",
    "query OrderQueries => OrderSummary",
    "readmodel OrderSummary",
    "reducer OrderSummaryProjection => OrderSummary",
):
    assert declaration in source, f"checked expectation is missing {declaration!r}"

generation = json.loads(file_diagnostics_bytes)
provenance = generation["provenance"]
assert provenance["provider"] == "critter-stack"
assert provenance["providerVersion"] == "0.23.0"
assert provenance["compatibility"] == {
    "supportTier": "Canonical",
    "recognitionStatus": "Recognized",
    "semanticConformance": "RequiresHumanReview",
    "loweringFidelity": "LossReported",
    "explanation": "Marten 9.23.0 with WolverineFx 6.29.1 matches a pinned canonical package set for bundled provider 0.23.0; only fixture-asserted behaviors are canonical",
}

projects = provenance["projects"]
assert [project["project"] for project in projects] == ["Domain", "Host"]
expected_projects = {
    "Domain": (
        "Integration/Cli/ScreenplayPlacement/Domain/Domain.csproj",
        "Integration/Cli/ScreenplayPlacement/Domain/Domain",
    ),
    "Host": (
        "Integration/Cli/ScreenplayPlacement/Host/Host.csproj",
        "Integration/Cli/ScreenplayPlacement/Host/Host",
    ),
}
canonical_packages = {
    "Marten": "9.23.0",
    "WolverineFx": "6.29.1",
    "WolverineFx.Marten": "6.29.1",
}
for project in projects:
    assert project["targetFramework"] == "net10.0"
    logical_path, identity = expected_projects[project["project"]]
    assert project["sourcePolicy"] == {
        "logicalProjectPath": logical_path,
        "projectIdentity": identity,
        "policyVersion": 1,
        "displayRoot": "Workspace",
        "casePolicy": "Ordinal",
    }
    source_structure = project["sourceStructure"]
    assert source_structure == {
        "projectRole": "Application",
        "policyVersion": 1,
        "featureRoot": "Source",
        "namespaceSegmentsToSkip": 1,
    }
    assert "module" not in source_structure, "an absent module override must stay absent"
    packages = {package["id"]: package["version"] for package in project["packages"]}
    assert {package: packages.get(package) for package in canonical_packages} == canonical_packages

expected_diagnostics = [
    (
        "warning",
        "GEN0004",
        "The recognized Aggregate artifact 'Order' cannot yet be represented by the Screenplay lowerer and was omitted",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Current/Current.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Current.Order",
        "Unsupported",
    ),
    (
        "warning",
        "GEN0004",
        "The recognized Message artifact 'OrderSubmitted' cannot yet be represented by the Screenplay lowerer and was omitted",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Notify/Notify.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Submit.OrderSubmitted",
        "Unsupported",
    ),
    (
        "warning",
        "GEN0004",
        "The recognized Message artifact 'SendOrderConfirmation' cannot yet be represented by the Screenplay lowerer and was omitted",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Notify/Notify.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Notify.SendOrderConfirmation",
        "Unsupported",
    ),
    (
        "warning",
        "GEN0004",
        "The recognized Reaction artifact 'OrderConfirmation' cannot yet be represented by the Screenplay lowerer and was omitted",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Notify/Notify.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Notify.OrderConfirmationHandler#method:M%3AScreenplayPlacement.Orders.Fulfillment.Notify.OrderConfirmationHandler.Handle%28ScreenplayPlacement.Orders.Fulfillment.Submit.OrderSubmitted%29:reaction",
        "Unsupported",
    ),
    (
        "warning",
        "GEN0004",
        "The recognized Aggregate artifact 'OrderSummary' cannot yet be represented by the Screenplay lowerer and was omitted",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Summary/Summary.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Summary.OrderSummary",
        "Unsupported",
    ),
    (
        "info",
        "WOLVERINE0002",
        "HTTP GET route '/orders/{id}' for query 'OrderQueries' is not represented by the current Screenplay language",
        "Integration/Cli/ScreenplayPlacement/Domain/Source/Orders/Fulfillment/Summary/Summary.cs",
        "dotnet://Domain/Domain/ScreenplayPlacement.Orders.Fulfillment.Summary.OrderQueries#method:M%3AScreenplayPlacement.Orders.Fulfillment.Summary.OrderQueries.GetOrder%28System.Guid%29:query",
        "Unsupported",
    ),
]
actual_diagnostics = [
    (
        diagnostic["severity"],
        diagnostic["code"],
        diagnostic["message"],
        diagnostic["location"],
        diagnostic["subject"],
        diagnostic["outcome"],
    )
    for diagnostic in generation["diagnostics"]
]
assert actual_diagnostics == expected_diagnostics

summary = json.loads(file_summary.read_bytes())
assert summary["path"] == file_output_path
assert summary["source"] == str(Path(repo_root) / "Integration/Cli/ScreenplayPlacement/Host/Host.csproj")
assert summary["projects"] == ["Domain", "Host"]
assert summary["diagnostics"] == len(expected_diagnostics)

validation = json.loads(validation_summary.read_bytes())
assert validation["path"] == expected_path
assert validation["files"] == 1
assert validation["diagnostics"] == 0


def assert_no_unexpected_physical_paths(value, allowed, location="$"):
    if isinstance(value, dict):
        for key, item in value.items():
            assert_no_unexpected_physical_paths(item, allowed, f"{location}.{key}")
    elif isinstance(value, list):
        for index, item in enumerate(value):
            assert_no_unexpected_physical_paths(item, allowed, f"{location}[{index}]")
    elif isinstance(value, str):
        if value in allowed:
            return
        for physical_root in (repo_root, work_root):
            assert physical_root not in value, f"{location} discloses physical root {physical_root}"


assert_no_unexpected_physical_paths(
    summary,
    {file_output_path, str(Path(repo_root) / "Integration/Cli/ScreenplayPlacement/Host/Host.csproj")},
)
assert_no_unexpected_physical_paths(validation, {expected_path})

print(f"Screenplay SHA-256: {hashlib.sha256(expected_bytes).hexdigest()}")
PY
