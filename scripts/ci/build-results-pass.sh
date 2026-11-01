#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Decides whether the Build workflow passes from the results of the jobs it depends on.
#
# Usage: build-results-pass.sh <render-legs-required> <render-job-id>...
# Stdin: the JSON of `toJSON(needs)`.
#
# Every job must have succeeded. The render jobs may instead be skipped, but only when
# <render-legs-required> is exactly `false`; any other value counts as required. Fails closed on
# unparsable input, missing jobs and unknown results.

set -euo pipefail

core_jobs=(build-pack specs)

if [[ $# -lt 1 ]]; then
    echo "Usage: build-results-pass.sh <render-legs-required> <render-job-id>..." >&2
    exit 2
fi

required=$1
shift
render_jobs=("$@")

input=$(cat)
if ! results=$(printf '%s' "$input" | jq -r 'if type == "object" then to_entries[] | "\(.key)\t\(.value.result // "")" else error("needs is not an object") end' 2>/dev/null); then
    echo "::error::The needs results are not a valid JSON object" >&2
    exit 1
fi

jobs=()
job_results=()
while IFS=$'\t' read -r job result; do
    [[ -z "$job" ]] && continue
    jobs+=("$job")
    job_results+=("$result")
    printf '%s: %s\n' "$job" "$result"
done <<< "$results"

has_job() {
    local candidate=$1 job
    for job in "${jobs[@]+"${jobs[@]}"}"; do
        [[ "$job" == "$candidate" ]] && return 0
    done
    return 1
}

is_render_job() {
    local candidate=$1 render
    for render in "${render_jobs[@]}"; do
        [[ "$render" == "$candidate" ]] && return 0
    done
    return 1
}

failed=0

for job in "${core_jobs[@]}" "${render_jobs[@]}"; do
    if ! has_job "$job"; then
        echo "::error::The job '$job' is missing from the needs results" >&2
        failed=1
    fi
done

for index in "${!jobs[@]}"; do
    job=${jobs[$index]}
    result=${job_results[$index]}
    if [[ "$result" == "success" ]]; then
        continue
    fi
    if [[ "$result" == "skipped" && "$required" == "false" ]] && is_render_job "$job"; then
        continue
    fi
    echo "::error::The job '$job' did not pass (result: ${result:-none})" >&2
    failed=1
done

exit "$failed"
