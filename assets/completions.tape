# Tab completion that goes and asks the server.
#
#   assets/demo-store/reset.sh
#   vhs assets/completions.tape
#
# The observer ids offered here are not a static word list baked into the completion script.
# The script shells back into `cratis _complete observers`, which opens a connection and
# returns what that server has registered at that moment.
#
# Recorded under zsh rather than bash: the generated bash script calls `_init_completion`,
# which comes from the bash-completion package, and the macOS system bash is 3.2 without it.

Output assets/completions.gif

Set Shell zsh
Source assets/_style.tape

Type "autoload -Uz compinit && compinit -u" Enter
Type "cratis completions zsh > /tmp/_cratis && source /tmp/_cratis" Enter
Type "compdef _cratis cratis" Enter
Type "export PROMPT='%F{75}❯%f '" Enter
Type "clear" Enter
Sleep 1s
Show

# `replay` reprocesses an observer from sequence zero. This tape never sends Enter — it ends
# on Ctrl+C, which abandons the line. Audit before rendering:  grep -n 'Enter' completions.tape
Type "cratis chronicle observers replay "
Sleep 1500ms
Tab
Sleep 4s

Type "Bookshop.O"
Sleep 1s
Tab
Sleep 1500ms
Tab
Sleep 4s

Ctrl+C
Sleep 2s
