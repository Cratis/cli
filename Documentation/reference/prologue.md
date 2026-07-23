# Prologue

`cratis prologue` captures what a running system *does* and interprets it into a Cratis Screenplay. [Prologue](https://github.com/Cratis/Prologue) points its extractor at an ordinary system — no Cratis constructs required — observes the HTTP commands, database changes, and telemetry flowing through it, and the CLI turns those captures into a `.play` file you continue authoring from.

```bash
cratis prologue start
cratis prologue interpret [PATH]
```

The typical flow: `start` writes a `cratis-prologue.json` capture configuration, you run the Prologue extractor container next to your system while exercising it, and `interpret` reads the captured `.jsonl` files and produces the Screenplay.

## `cratis prologue start`

An interactive wizard that produces the `cratis-prologue.json` configuration the Prologue extractor reads. It generates a fresh Prologue id and walks through:

1. **Sources** — a multi-select of what to capture:
   - **SQL Server** — one or more databases, each with a name, connection string, and an optional table allowlist (comma separated, empty captures every user table). The extractor enables Change Data Capture itself.
   - **PostgreSQL** — one or more databases, each with a name and connection string. Captured through logical replication with default slot and publication names.
   - **API** — the extractor sits in front of your system as a reverse proxy and observes the state-changing HTTP commands (`POST`, `PUT`, `DELETE`) flowing through it. You provide the base path (default `/api`) and the address of your system.
   - **OpenTelemetry** — the extractor acts as an OTLP collector (HTTP and gRPC). You provide service names to capture (empty captures all), attribute keys whose values are captured, and optional upstream collectors to forward telemetry to (empty makes it a terminal collector).
2. **Output** — where captured data goes: rolling JSON capture files (default directory `./captures`, ready for `cratis prologue interpret`) or the Prologue Receiver API (default endpoint `http://localhost:5005`).

When done it writes the file, prints a summary table, and shows how to run the extractor container.

| Option | Description |
|---|---|
| `--file <PATH>` | Where to write the configuration — a file path or a directory. Defaults to `cratis-prologue.json` in the current directory. |

```bash
cratis prologue start
cratis prologue start --file ./my-system
```

The wizard requires an interactive terminal. In CI, with piped input, or with `-y/--yes` it fails with a validation error — write the configuration by hand instead.

## `cratis prologue interpret [PATH]`

Reads Prologue capture (`.jsonl`) files and interprets them into a Screenplay. Deterministic heuristics build the event model's structure from the evidence — commands, events, read models, projections, and constraints per module, feature, and slice. When a language model is configured it refines the names into domain language, derives the system name, and may ask you clarifying questions.

| Argument | Description |
|---|---|
| `PATH` | Folder holding the capture (`.jsonl`) files. Defaults to the configured JSON output directory when a `cratis-prologue.json` is found in `PATH` or the current directory, otherwise the current directory. |

| Option | Description |
|---|---|
| `--file <FILE>` | File to write the generated Screenplay to. Defaults to `<SystemName>.play` in the current directory. |
| `--prologue-id <ID>` | The Prologue the captures belong to. Defaults from `cratis-prologue.json` when one is present. |

```bash
cratis prologue interpret
cratis prologue interpret ./captures
cratis prologue interpret ./captures --file MySystem.play
```

On success it prints a panel with the written path, the derived system name, and the module/feature/slice counts — and the natural next step is `cratis run` to boot the Screenplay in a local [Stage](run.md) sandbox.

### Language model refinement

The language model is resolved in this order:

1. The `llm` section of a found `cratis-prologue.json`, when it is enabled there.
2. The `llm` section of `~/.cratis/config.json`, written by [`cratis llm use`](llm.md) — `anthropic`, `openai`, or `local` (OpenAI-compatible).
3. Neither configured — interpretation runs with heuristics only and an info line points at `cratis llm use`.

When the language model is genuinely uncertain about a decision that materially changes the model, it asks questions — one at a time, each with its background context, a list of choices, and always an "Other" entry for typing your own answer. Questions are only asked in an interactive terminal; non-interactive runs (CI, piped output, `-y/--yes`) never ask and finalize with the model's best effort.

## Errors

| Condition | Result |
|---|---|
| `start` without an interactive terminal, or with `--yes` | Validation error — the wizard needs a terminal. |
| `interpret` finds no capture (`.jsonl`) files in the folder | Not-found error with a hint to run the extractor with JSON output. |
| Interpretation fails | Server error carrying the session's error message. |
