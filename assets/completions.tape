# Tab completion that goes and asks the server.
#
#   assets/record.sh
#
# The read model names offered here are not a static word list baked into the completion
# script. The script shells back into `cratis _complete read-models`, which opens a connection
# and returns what that server has registered at that moment.
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

Type "cratis chronicle read-models instances "
Sleep 1500ms
Tab
Sleep 4s

# The first Tab inserted the common prefix `Bookshop.` shared by all four. `Bor` is the
# shortest string that picks one of them, so the second Tab can complete it in full.
Type "Bor"
Sleep 1s
Tab
Sleep 2s
Enter
Sleep 6s
