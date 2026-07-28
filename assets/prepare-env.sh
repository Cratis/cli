#!/bin/bash
# Prepares the recording environment. Called from inside the Hide block of _style.tape,
# so nothing here appears on camera.
#
#   - a `cratis` on PATH that runs the local Release build
#   - an isolated $HOME/.cratis pointing at the demo server
#   - an empty working directory, so the prompt shows a neutral path
#
# The update hint is silenced with CRATIS_NO_UPDATE_CHECK, exported by _style.tape.
set -e

REPO="$(cd "$(dirname "$0")/.." && pwd)"
BIN="$REPO/assets/.recording-bin"
CRATIS_HOME="${HOME:?}/.cratis"

mkdir -p "$BIN" "$CRATIS_HOME" /tmp/bookshop

cat > "$BIN/cratis" <<EOF
#!/bin/bash
exec "$REPO/Source/Cli/bin/Release/net10.0/Cratis.Cli" "\$@"
EOF
chmod +x "$BIN/cratis"

cat > "$CRATIS_HOME/config.json" <<'EOF'
{
  "activeContext": "local",
  "contexts": {
    "local": {
      "server": "chronicle://localhost:35100/",
      "eventStore": "Bookshop",
      "namespace": null,
      "clientId": null,
      "clientSecret": null,
      "accessToken": null,
      "tokenExpiry": null,
      "loggedInUser": null
    }
  }
}
EOF

# The workbench reopens on the view it was last left on. vhs cannot deliver arrow keys on this
# machine, so rather than navigating on camera the clips start where they want to be: Observers,
# view index 1. RECORDING_NAV_INDEX picks a different one.
cat > "$CRATIS_HOME/workbench-state.json" <<EOF
{
  "Interval": 5,
  "LastNavIndex": ${RECORDING_NAV_INDEX:-1}
}
EOF
