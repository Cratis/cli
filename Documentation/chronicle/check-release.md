---
title: Check a release against Cratis Chronicle with the CLI
description: Check read-only that a release is compatible with a Chronicle server before you publish or deploy it, and gate a pipeline on the cratis.compat/v1 result.
---

<a id="check-release"></a>

Use `check-release` as a gate before publishing or deploying a release. It reads the Chronicle server you select and reports whether the release can work against it: the kernel version, the wire contracts of the release, and the event type registrations the release would make.

The command is read-only. The event type check is the server's validate-only operation: nothing is registered, normalized, or repaired, and no event is appended.

## Usage

```bash
cratis chronicle check-release --release release.json --event-store Shop --output json
```

| Flag | Description |
|---|---|
| `--release <FILE>` | Path of the release description JSON file. Required. |
| `-e`, `--event-store <NAME>` | Event store the registrations are checked against. Defaults to the active context's event store, then `default`. It is never prompted for. |
| `--server <CONNECTION_STRING>` | Chronicle server to check. Without it the CLI uses `CHRONICLE_CONNECTION_STRING`, then the active context. |
| `-o`, `--output <FORMAT>` | Output format. `json` prints exactly one `cratis.compat/v1` document on standard output. |

## The release description

The release supplies its own facts. The CLI never reads its own contracts or infers anything from a version number, because that would say whether the CLI is compatible, not your release.

```json
{
  "release": "v3.2.0",
  "repository": "Cratis/Direct",
  "component": "direct",
  "environment": "production",
  "minimumKernelVersion": "19.157.0",
  "client": { "type": "dotnet", "version": "19.181.3", "protocolVersion": 1 },
  "descriptorSet": "<base64 of the descriptor set built from the release's own contracts artifact>",
  "eventTypes": [
    { "type": { "id": "BookAdded", "generation": 1 }, "schema": "{ ... JSON schema as a string ... }" }
  ]
}
```

| Field | Required | Meaning |
|---|---|---|
| `release` | Yes | Name of the release, for example its tag. |
| `client.type`, `client.version`, `client.protocolVersion` | Yes | What the release identifies as when it connects. |
| `descriptorSet` | Yes | The wire contract descriptor set of the release, as non-empty base64. Extract it from the contracts artifact the release ships, not from the CLI. |
| `eventTypes` | Yes | The event type registrations the release would make, each with its full `type` (`id`, `generation`) and `schema` string. List each event type once with all generations in the entry. An empty array is valid. |
| `minimumKernelVersion` | No | The oldest kernel version the release supports. Without it the kernel check reports that no minimum was specified. |
| `repository`, `component`, `environment` | No | Provenance shown in the report. They are only reported when you supply them. |

A file that is unreadable, is not JSON, or breaks one of these rules ends the command with exit code `5` and an error naming the field. Nothing is contacted.

## Output

The `json` output is the shared `cratis.compat/v1` result: the same document Cratis Direct's release gate writes, as defined by its released `compat-result` contract (v3.111.0). It adds one field, `format`, with the same value as `schema`.

```json
{
  "schema": "cratis.compat/v1",
  "format": "cratis.compat/v1",
  "subject": { "repo": "Cratis/Direct", "release": "v3.2.0", "component": "direct" },
  "production": { "environment": "production", "readAt": "2026-11-23T10:00:00Z" },
  "eventStore": "Shop",
  "verdict": "incompatible",
  "checks": [
    { "check": "chronicle-kernel", "expected": "kernel 19.157.0 or newer", "actual": "kernel 19.181.3, protocol 1", "verdict": "compatible", "action": "" },
    { "check": "chronicle-contracts", "expected": "dotnet 19.181.3, protocol 1", "actual": "server 19.181.3, protocol 1", "verdict": "compatible", "action": "" },
    { "check": "event-schemas", "expected": "1 event type registration(s) accepted by event store 'Shop'", "actual": "BookAdded generation 1 rejected at /title (required): Property removed", "verdict": "incompatible", "action": "Make the schema backward compatible, add a generation with a migration, or follow the reviewed schema registry process; do not disable schema validation." }
  ],
  "release": "v3.2.0"
}
```

`production.readAt` is the UTC time the server was read. There are always exactly three checks, in this order:

| Check | Compatible when |
|---|---|
| `chronicle-kernel` | The server reports a version that is at least `minimumKernelVersion`. |
| `chronicle-contracts` | The server accepts the release's descriptor set and client identity. Incompatibilities the server lists are included in `actual`. |
| `event-schemas` | The server accepted every registration and returned a result for each one. |

A check that could not be answered, such as a server that does not offer the operation or does not return a result for an event type, is `unknown`, never `compatible`. The overall verdict is `incompatible` if any check is, else `unknown` if any check is, else `compatible`. Every check that is not compatible has a non-empty `action`.

## Exit codes

| Exit code | Meaning |
|---|---|
| `0` | Compatible. |
| `5` | Incompatible, or the release file or options are invalid. |
| `3` | Unknown: the server was reached but a check could not be answered, or the server reported an error. |
| `2` | The Chronicle server could not be reached. Reported as an all-`unknown` result. |
| `4` | The server refused the credentials or the login expired. Reported as an all-`unknown` result. |

These are the CLI's own exit codes. Cratis Direct's script uses different numbers (`10` incompatible, `11` unknown), so gate on non-zero rather than on a specific value across the two.

## Gate a release in GitHub Actions

Run the check before anything is published or deployed, and let a non-zero exit stop the pipeline. Pin the CLI version and the actions, grant the job the minimum permissions, and pass the connection string through the environment, never into the script text.

```yaml
jobs:
  check-release:
    runs-on: ubuntu-latest
    timeout-minutes: 10
    permissions:
      contents: read
    steps:
      - uses: actions/checkout@<full-commit-sha> # pinned
      - uses: actions/setup-dotnet@<full-commit-sha> # pinned
        with:
          dotnet-version: 10.0.x
      - uses: actions/download-artifact@<full-commit-sha> # pinned
        with:
          name: release-manifest
      - run: dotnet tool install -g Cratis.Cli --version <pinned-version>
      - run: cratis chronicle check-release --release release.json --event-store Shop --output json
        env:
          CHRONICLE_CONNECTION_STRING: ${{ secrets.CHRONICLE_CONNECTION_STRING }}
```

Do not add `continue-on-error`, and do not disable the workflow or dispatch around a held release. Resolve the check named in the report, then run the pipeline again; the CLI does not retry or remediate.

## Limitations

The result is a snapshot of the server at `production.readAt`. It is not a reservation: the server can change after the check. Only the event store you select is checked, and the CLI does not interpret schemas itself, so what counts as compatible is decided by the Chronicle server.
