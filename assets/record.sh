#!/bin/bash
# Renders every README GIF from scratch.
#
#   assets/record.sh              # all of them
#   assets/record.sh demo         # just one
#
# Each tape needs a particular server state, and the difference matters: three of the four are
# recorded against a live, connected client so observers report Active, and only triage is
# recorded with a fault armed. Getting that wrong produces a set where everything looks broken.
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
cd "$REPO"

[ -x Source/Cli/bin/Release/net10.0/Cratis.Cli ] || dotnet build -c Release

stop_client() { pkill -f 'bin/Release/net10.0/Bookshop' 2>/dev/null || true; }
trap stop_client EXIT

# Waits for the client's observers to report Active before rolling, otherwise the first frames
# catch a store that is still connecting.
start_client() {
    stop_client
    nohup "$HERE/demo-store/reset.sh" "$1" 600 >/dev/null 2>&1 &
    printf 'connecting'
    for _ in $(seq 1 40); do
        if HOME="$HERE/.recording-home" CRATIS_NO_UPDATE_CHECK=1 \
           "$HERE/.recording-bin/cratis" chronicle observers list -o plain 2>/dev/null \
           | grep -q 'Bookshop.*Active'; then
            echo; return
        fi
        printf '.'; sleep 2
    done
    echo; echo "warning: client never reported Active" >&2
}

cli() {
    HOME="$HERE/.recording-home" CRATIS_NO_UPDATE_CHECK=1 "$HERE/.recording-bin/cratis" "$@"
}

# Renders are expensive and their failures are silent — a clip against the wrong state still
# produces a perfectly good-looking GIF of the wrong thing. So the state is asserted first.
require_state() {
    local want=$1 n=0
    printf 'waiting for %s state' "$want"
    while [ $n -lt 30 ]; do
        local failures
        failures=$(cli chronicle failed-partitions list -q 2>/dev/null | grep -c . || true)
        case "$want" in
            healthy) [ "$failures" = "0" ] && { echo; return; } ;;
            failing) [ "$failures" != "0" ] && { echo; return; } ;;
        esac
        printf '.'; sleep 2; n=$((n + 1))
    done
    echo
    echo "error: store never reached '$want' state — refusing to record it" >&2
    exit 1
}

render() {
    echo "── $1"
    vhs "$HERE/$1.tape"
}

"$HERE/demo-store/reset.sh"
HOME="$HERE/.recording-home" bash "$HERE/prepare-env.sh"

want() { [ $# -eq 0 ] || [ -z "$1" ] || [ "$1" = "$2" ]; }

if want "$1" demo || want "$1" workbench || want "$1" completions; then
    # A healthy client clears the seeded failure through Chronicle's own retry, which is
    # exactly the state these three want.
    start_client serve
    require_state healthy
    want "$1" demo       && render demo
    want "$1" workbench  && render workbench
    want "$1" completions && render completions
fi

if want "$1" triage; then
    # A fresh store, then the fault armed before the client connects — Chronicle retries a
    # pending failed partition the moment a client reconnects, so a late arm lets that first
    # retry succeed and clears the very failure this clip is about.
    "$HERE/demo-store/reset.sh"
    start_client serve-failing
    require_state failing
    render triage
fi

echo "done"
