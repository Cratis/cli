# The one clip about something being wrong: a single failing partition in an otherwise
# healthy store, and the trail from the verdict to the exception that caused it.
#
#   assets/record.sh
#
# Recorded against `reset.sh serve-failing`, so the client is connected and every other
# observer is Active. The only red thing on screen is the actual problem.

Output assets/triage.gif

Set Shell bash
Source assets/_style.tape

Type `export PS1='\[\033[38;2;100;180;255m\]❯\[\033[0m\] '` Enter
Type "clear" Enter
Sleep 1s
Show

Type "cratis chronicle diagnose" Enter
Sleep 6s

Type "cratis chronicle failed-partitions list" Enter
Sleep 5s

# -o table is explicit because the pipe makes the CLI think a machine is reading and switch
# to JSON — correct behaviour, wrong for a demo. head keeps the first attempt on screen.
Type "cratis chronicle failed-partitions show Bookshop.OverdueNotices 978-0131177055 -o table | head -16" Enter
Sleep 8s
