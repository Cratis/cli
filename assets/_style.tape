# Shared by every tape in this directory via `Source assets/_style.tape`.
# One file so the set reads as one recording rather than three screencasts.
#
# This file does NOT pick the shell and does NOT close the Hide block. Each tape sets its own
# `Set Shell` before sourcing this, then finishes the hidden setup with a prompt and `Show` —
# because a bash prompt and a zsh prompt are not written the same way, and completions.tape
# needs zsh.

Set FontSize 14
Set Width 1400
Set Height 800
Set Framerate 24
Set TypingSpeed 55ms
Set Padding 20
Set Margin 24
Set MarginFill "#05070f"
Set BorderRadius 10
Set WindowBar Colorful

# Hand-matched to the application's own palette — WorkbenchColors.cs for the workbench and
# OutputFormatter.cs for everything else — so the terminal chrome and the CLI agree.
Set Theme { "background": "#080c1c", "foreground": "#dce1fa", "cursor": "#6387ff", "selection": "#234696", "black": "#080c1c", "red": "#ff5555", "green": "#50c878", "yellow": "#ffb74d", "blue": "#6387ff", "magenta": "#c89bff", "cyan": "#3cdcc8", "white": "#dce1fa", "brightBlack": "#5a6ea0", "brightRed": "#ff6482", "brightGreen": "#64dc82", "brightYellow": "#f0be50", "brightBlue": "#64b4ff", "brightMagenta": "#c89bff", "brightCyan": "#3cdcc8", "brightWhite": "#ffffff" }

Hide

# The CLI detects agent environments and silently switches to compact JSON, drops the banner
# and drops the update hint. Recording from inside an agent session without this produces a
# GIF of machine output. See GlobalSettings.IsAiAgentEnvironment().
Type "unset CLAUDECODE CLAUDE_CODE_ENTRYPOINT CLAUDE_CODE_SSE_PORT CURSOR_TRACE_DIR WINDSURF_SESSION_ID" Enter

# Without this every command in every clip would end in `↑ update available`.
Type "export CRATIS_NO_UPDATE_CHECK=1" Enter

# An isolated home, because the CLI reads $HOME/.cratis/config.json and a real one holds real
# server addresses and client secrets.
Type "export CRATIS_REPO=$PWD" Enter
Type "export HOME=$CRATIS_REPO/assets/.recording-home" Enter
Type "export PATH=$CRATIS_REPO/assets/.recording-bin:$PATH" Enter
Type "bash $CRATIS_REPO/assets/prepare-env.sh" Enter
Type "cd /tmp/bookshop" Enter
