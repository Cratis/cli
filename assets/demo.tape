# Hero. The arc this tool exists for: a verdict, the event that caused it, then the live view.
#
#   assets/demo-store/reset.sh     # seed the store and leave one partition failing
#   vhs assets/demo.tape
#
# Nothing here is driven by arrow keys. vhs cannot deliver them to the application on the
# recording machine — bash's own history does not respond to Up either — so the workbench
# segment uses only keys that verifiably arrive: F, printable characters, Escape and q.

Output assets/demo.gif

Set Shell bash
Source assets/_style.tape

Type `export PS1='\[\033[38;2;100;180;255m\]❯\[\033[0m\] '` Enter
Type "clear" Enter
Sleep 1s
Show

Type "cratis chronicle diagnose" Enter
Sleep 6s

# A partition id is 36 characters of nothing happening at reading speed, so this one line
# is typed fast. Everything the viewer is meant to read is at the default speed.
Set TypingSpeed 18ms
Type "cratis chronicle failed-partitions show Bookshop.OverdueNotices 00000014-1111-4222-8333-444444444444" Enter
Set TypingSpeed 55ms
Sleep 7s

Type "clear" Enter
Sleep 500ms

Type "cratis chronicle workbench" Enter
Sleep 9s

# F opens the filter on whichever view is showing. Typed slowly here because the narrowing
# is the content.
Type "F"
Sleep 1500ms
Set TypingSpeed 140ms
Type "Bookshop"
Set TypingSpeed 55ms
Sleep 4s

Escape
Sleep 1500ms
Type "q"
Sleep 2s
