# The workbench: filtering a view, then searching every artifact kind at once.
#
#   assets/record.sh
#
# Not driven by arrow keys — vhs cannot deliver them to the application on the recording
# machine, and bash's own history does not respond to Up either. Only keys that verifiably
# arrive are used: F, printable characters, Escape and q.

Output assets/workbench.gif

Set Shell bash
Source assets/_style.tape

Type `export PS1='\[\033[38;2;100;180;255m\]❯\[\033[0m\] '` Enter
Type "clear" Enter
Sleep 1s
Show

Type "cratis chronicle workbench" Enter
Sleep 9s

# F filters whichever view is showing. Typed slowly because the narrowing is the content.
Type "F"
Sleep 1200ms
Set TypingSpeed 140ms
Type "Bookshop"
Set TypingSpeed 55ms
Sleep 3500ms
Escape
Sleep 1500ms

# Ctrl+P searches across every kind at once.
Ctrl+P
Sleep 1500ms
Set TypingSpeed 150ms
Type "Overdue"
Set TypingSpeed 55ms
Sleep 4500ms

Escape
Sleep 1s
Type "q"
Sleep 1500ms
