#!/bin/bash
# Rebuild the event store the README GIFs are recorded against.
#
# Starts a throwaway Chronicle server, connects the Bookshop client, and appends a fixed story.
# Identifiers are deterministic, so every re-render shows the same ids as the committed GIFs.
#
#   ./reset.sh                     # fresh server, seed, exit
#   ./reset.sh serve 90            # stay connected and healthy for 90s
#   ./reset.sh serve-failing 90    # stay connected with one partition still failing
#   ./reset.sh drip 40             # append a trickle against the running server
#
# The serve modes matter for recording. A client that has exited leaves every observer
# Disconnected, which looks like a broken system when it is only a stopped process.
#
# Deliberately NOT Chronicle's default 35000. Anything else on this machine — an Aspire
# AppHost's control plane, a compose stack — that has claimed 127.0.0.1:35000 wins the name
# `localhost`, and this script would then health-check and seed somebody else's server.
# Override with CHRONICLE_PORT only if you are sure 35000 is yours.
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
PORT="${CHRONICLE_PORT:-35100}"
CONNECTION="chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:$PORT/"
CONTAINER="chronicle-readme"
IMAGE="cratis/chronicle:16.38.1-development"
BOOKSHOP="$HERE/bin/Release/net10.0/Bookshop"

[ -x "$BOOKSHOP" ] || dotnet build "$HERE/DemoStore.csproj" -c Release

case "$1" in
    serve|serve-failing|drip)
        exec "$BOOKSHOP" "$CONNECTION" Bookshop "${2:-60}" "$1"
        ;;
esac

docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" -p "$PORT":35000 -p 30100:30000 -p 11100:11111 "$IMAGE" >/dev/null

printf 'waiting for the server'
for _ in $(seq 1 60); do
    if [ "$(curl -sk -o /dev/null -w '%{http_code}' https://localhost:$PORT/health 2>/dev/null)" = "200" ]; then
        break
    fi
    printf '.'
    sleep 1
done
echo

sleep 2
"$BOOKSHOP" "$CONNECTION" Bookshop 20 seed
