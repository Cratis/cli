#!/bin/bash
# Rebuild the event store the README GIFs are recorded against.
#
# Starts a throwaway Chronicle server, connects the Bookshop client, and appends a fixed
# story that ends with one reactor partition failing. Identifiers are deterministic, so
# every re-render of the tapes shows the same ids as the committed GIFs.
#
#   ./reset.sh            # fresh server, seed, exit
#   ./reset.sh drip 40    # append a trickle for 40s against the running server
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
PORT="${CHRONICLE_PORT:-35100}"
CONNECTION="chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:$PORT/"
CONTAINER="chronicle-readme"
IMAGE="cratis/chronicle:16.7.0-development"
BOOKSHOP="$HERE/bin/Release/net10.0/Bookshop"

[ -x "$BOOKSHOP" ] || dotnet build "$HERE/DemoStore.csproj" -c Release

if [ "$1" = "drip" ]; then
    exec "$BOOKSHOP" "$CONNECTION" Bookshop "${2:-40}" drip
fi

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
