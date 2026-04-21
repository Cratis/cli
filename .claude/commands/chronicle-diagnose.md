# Chronicle Diagnose

Run a diagnostic check on the connected Chronicle server.

## Steps

1. Run `cratis version -o json` to check connectivity and version compatibility.
2. Run `cratis observers list -o plain` to check observer states.
3. Run `cratis failed-partitions list -o plain` to find failing partitions.
4. If there are failed partitions, run `cratis failed-partitions show <observer-id> <partition> -o json` for each to get error details.
5. Run `cratis recommendations list -o plain` to check for pending recommendations.

## Output

Summarize findings:
- Server connectivity and version compatibility
- Number of observers and their states
- Any failed partitions with error summaries
- Any pending recommendations
- Suggested remediation steps