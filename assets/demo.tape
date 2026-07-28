# Hero. The three questions you actually open this tool to answer: is it healthy, what
# happened, and what state did that produce.
#
#   assets/record.sh          # sets up the store and renders every tape
#
# Recorded against a live, connected client so observers report Active. A client that has
# exited leaves everything Disconnected, which looks like a broken system when it is only a
# stopped process.

Output assets/demo.gif

Set Shell bash
Source assets/_style.tape

Type `export PS1='\[\033[38;2;100;180;255m\]❯\[\033[0m\] '` Enter
Type "clear" Enter
Sleep 1s
Show

Type "cratis chronicle diagnose" Enter
Sleep 6s

Type "clear" Enter
Sleep 300ms
Type "cratis chronicle events get --from 11" Enter
Sleep 7s

Type "clear" Enter
Sleep 300ms
Type "cratis chronicle read-models instances Bookshop.BorrowedBook" Enter
Sleep 8s
