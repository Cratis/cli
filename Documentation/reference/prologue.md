# Prologue

`cratis prologue` captures what a running system *does* and interprets it into a Cratis Screenplay. [Prologue](/prologue/) points its extractor at an ordinary system — no Cratis constructs required — observes the HTTP commands, database changes, and telemetry flowing through it, and the CLI turns those captures into a `.play` file you continue authoring from.

```bash
cratis prologue start
cratis prologue interpret [PATH]
```

The typical flow: `start` writes a `cratis-prologue.json` capture configuration, you run the Prologue extractor container next to your system while exercising it, and `interpret` reads the captured `.jsonl` files and produces the Screenplay. This page covers the two commands; for what the extractor actually watches, how captures get correlated, and every `cratis-prologue.json` property the wizard can write, see the [Prologue documentation](/prologue/) — in particular [how Prologue works](/prologue/concepts/) and the full [configuration reference](/prologue/reference/configuration/).

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

Reads Prologue capture (`.jsonl`) files and interprets them into a Screenplay. Deterministic heuristics build the event model's structure from the evidence — commands, events, read models, projections, and constraints per module, feature, and slice (see [the extraction result](/prologue/reference/extraction-result/) for the exact shape). When a language model is configured it refines the names into domain language, derives the system name, and may ask you clarifying questions.

| Argument | Description |
|---|---|
| `PATH` | Folder holding the capture (`.jsonl`) files. Defaults to the configured JSON output directory when a `cratis-prologue.json` is found in `PATH` or the current directory, otherwise the current directory. |

| Option | Description |
|---|---|
| `--file <FILE>` | File to write the generated Screenplay to. Defaults to `<SystemName>.play` in the current directory. |
| `--prologue-id <ID>` | The Prologue the captures belong to. Defaults from `cratis-prologue.json` when one is present. |
| `--no-llm` | Force heuristics-only interpretation. Never creates a chat client or sends capture evidence to a model, regardless of local or global configuration. |

```bash
cratis prologue interpret
cratis prologue interpret ./captures
cratis prologue interpret ./captures --file MySystem.play
cratis prologue interpret ./captures --no-llm
```

On success it prints a panel with the written path, the derived system name, and the module/feature/slice counts — and the natural next step is `cratis run` to boot the Screenplay in a local [Stage](run.md) sandbox.

### Language model refinement

The language model is resolved in this order:

1. `--no-llm` disables refinement, overriding every configuration setting.
2. An explicit `llm.enabled` in a found `cratis-prologue.json`: `true` uses the local provider; `false` disables refinement **without falling back to the global provider**.
3. When the local `llm.enabled` setting is absent, the `llm` section of `~/.cratis/config.json`, written by [`cratis llm use`](llm.md) — `anthropic`, `openai`, or `local` (OpenAI-compatible).
4. Neither configured — interpretation runs with heuristics only.

With `--no-llm` or local `llm.enabled: false`, no chat client is created and no capture evidence leaves the machine for a model. Use `--no-llm` for scripts that must never contact a model.

Before creating the interpreter session, the command prints the effective provider kind, model id, endpoint host, and setting source to **stderr**. This notice is also printed with `-y/--yes`, JSON output, `--quiet`, or no terminal; a globally configured model is not silently used. Only the endpoint host is shown, never credentials, paths, or query strings. When refinement is disabled, the notice says `none` and heuristics-only mode.

For Anthropic, a custom endpoint takes precedence over `ANTHROPIC_BASE_URL`; an empty endpoint or the default Ollama URL (`http://llm:11434`) is treated as unset. When neither a custom endpoint nor the environment variable is set, the public Anthropic API is used. The resolved endpoint is fixed before the notice and passed to the client. The OpenAI provider uses its public API; other providers use their configured endpoint. An enabled provider's effective endpoint must be an absolute HTTP or HTTPS URL with a host (for example, `http://127.0.0.1:11434`); an invalid endpoint causes a validation error before any capture evidence is sent.

Table and plain results include the provider notice. JSON results include an `llm` object with `used`, `kind`, `model`, `endpointHost`, and `source`, including when no model is used. `source` is `local file`, `global config`, `--no-llm`, or `none`; a disabled model has `used: false`, `kind: "none"`, and empty model and endpoint host values. `used` indicates that refinement was enabled for the run, not that a provider returned a usable refinement. Quiet text output remains the written file path; the notice still appears on stderr.

When the language model is genuinely uncertain about a decision that materially changes the model, it asks questions — one at a time, each with its background context, a list of choices, and always an "Other" entry for typing your own answer. Questions are only asked in an interactive terminal; non-interactive runs (CI, piped output, `-y/--yes`) never ask and finalize with the model's best effort.

## Errors

| Condition | Result |
|---|---|
| `start` without an interactive terminal, or with `--yes` | Validation error — the wizard needs a terminal. |
| `interpret` finds no capture (`.jsonl`) files in the folder | Not-found error with a hint to run the extractor with JSON output. |
| Enabled model's effective endpoint is invalid | Validation error before creating a chat client or sending capture evidence. |
| Interpretation fails | Server error carrying the session's error message. |

## Running Prologue without the CLI

These two commands wrap the Prologue extractor and interpreter containers — writing `cratis-prologue.json` by hand and running `docker run` yourself works exactly the same way, and is what you'd do to run the extractor as a long-lived sidecar rather than a one-off wizard session. See [Point Prologue at your system](/prologue/guides/point-prologue-at-your-system/) and [Running the Interpreter](/prologue/guides/running-the-interpreter/) in the Prologue documentation.
