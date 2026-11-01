#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)

render_ids=(rendered-react packaged-placement-and-axon application-shell)

# assert_pass <name> <pass|fail> <required> <json> [render-job-id...]
assert_pass() {
    local name=$1 expected=$2 required=$3 json=$4 status=0
    shift 4
    printf '%s' "$json" | bash "$script_dir/build-results-pass.sh" "$required" "$@" > /dev/null 2>&1 || status=$?
    local actual=pass
    [[ "$status" -ne 0 ]] && actual=fail
    if [[ "$actual" != "$expected" ]]; then
        printf 'FAIL %s: expected %s, got %s (exit %s)\n' "$name" "$expected" "$actual" "$status" >&2
        exit 1
    fi
    printf 'PASS %s\n' "$name"
}

all_success='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"success"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"}}'
render_skipped='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"skipped"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"skipped"}}'
all_render_skipped='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"skipped"},"packaged-placement-and-axon":{"result":"skipped"},"application-shell":{"result":"skipped"}}'
pack_failed='{"build-pack":{"result":"failure"},"specs":{"result":"skipped"},"rendered-react":{"result":"skipped"},"packaged-placement-and-axon":{"result":"skipped"},"application-shell":{"result":"skipped"}}'
specs_cancelled='{"build-pack":{"result":"success"},"specs":{"result":"cancelled"},"rendered-react":{"result":"success"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"}}'
specs_skipped='{"build-pack":{"result":"success"},"specs":{"result":"skipped"},"rendered-react":{"result":"success"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"}}'
extra_skipped='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"success"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"},"extra":{"result":"skipped"}}'
render_failed='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"failure"},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"}}'
render_missing='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{"result":"success"},"application-shell":{"result":"success"}}'
no_result='{"build-pack":{"result":"success"},"specs":{"result":"success"},"rendered-react":{},"packaged-placement-and-axon":{"result":"success"},"application-shell":{"result":"success"}}'

assert_pass 'everything succeeded, required' pass true "$all_success" "${render_ids[@]}"
assert_pass 'render jobs skipped, not required' pass false "$render_skipped" "${render_ids[@]}"
assert_pass 'all render jobs skipped, not required' pass false "$all_render_skipped" "${render_ids[@]}"
assert_pass 'render job succeeded, not required' pass false "$all_success" "${render_ids[@]}"
assert_pass 'render job skipped, required' fail true "$render_skipped" "${render_ids[@]}"
assert_pass 'pack failed, rest skipped' fail true "$pack_failed" "${render_ids[@]}"
assert_pass 'pack failed, not required' fail false "$pack_failed" "${render_ids[@]}"
assert_pass 'specs cancelled' fail true "$specs_cancelled" "${render_ids[@]}"
assert_pass 'specs skipped, not required' fail false "$specs_skipped" "${render_ids[@]}"
assert_pass 'unknown job skipped' fail false "$extra_skipped" "${render_ids[@]}"
assert_pass 'render job failed, not required' fail false "$render_failed" "${render_ids[@]}"
assert_pass 'required is empty' fail '' "$render_skipped" "${render_ids[@]}"
assert_pass 'required is maybe' fail maybe "$render_skipped" "${render_ids[@]}"
assert_pass 'required is uppercase false' fail FALSE "$render_skipped" "${render_ids[@]}"
assert_pass 'empty stdin' fail true '' "${render_ids[@]}"
assert_pass 'invalid JSON' fail true 'not json' "${render_ids[@]}"
assert_pass 'JSON is not an object' fail true '[]' "${render_ids[@]}"
assert_pass 'empty object' fail false '{}' "${render_ids[@]}"
assert_pass 'named render job missing from needs' fail true "$render_missing" "${render_ids[@]}"
assert_pass 'job without a result' fail true "$no_result" "${render_ids[@]}"
