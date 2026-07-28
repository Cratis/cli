# One query, every kind of artifact that touches it.
#
#   assets/demo-store/reset.sh
#   vhs assets/palette.tape

Output assets/palette.gif

Set Shell bash
Source assets/_style.tape

Type `export PS1='\[\033[38;2;100;180;255m\]❯\[\033[0m\] '` Enter
Type "clear" Enter
Sleep 1s
Show

Type "cratis chronicle workbench" Enter
Sleep 9s

Ctrl+P
Sleep 2s

# Typed slowly: the results resolving as the query narrows is the whole point of the clip.
Set TypingSpeed 160ms
Type "Overdue"
Set TypingSpeed 55ms
Sleep 5s

Escape
Sleep 1s
Type "q"
Sleep 1500ms
