<div align="center">
  <a href="https://www.cratis.io/cli/">
    <img src="https://raw.githubusercontent.com/Cratis/cli/main/cratis.svg" alt="Cratis" width="420" style="background-color: white">
  </a>

  <p align="center">
    <a href="https://www.nuget.org/packages/Cratis.Cli"><img src="https://img.shields.io/nuget/v/Cratis.Cli?logo=nuget" alt="NuGet"></a>
    <a href="https://github.com/Cratis/cli/releases/latest"><img src="https://img.shields.io/github/v/release/Cratis/cli?color=86efac" alt="Release"></a>
    <a href="https://discord.gg/kt4AMpV8WV"><img src="https://img.shields.io/discord/1182595891576717413?label=Discord&logo=discord&color=7289da" alt="Discord"></a>
    <a href="#license"><img src="https://img.shields.io/badge/license-MIT-green" alt="License"></a>
  </p>

  <img src="https://raw.githubusercontent.com/Cratis/cli/main/assets/demo.gif" alt="The cratis CLI showing Chronicle diagnostic output, events, and a projected read model" width="900">

  <sub>Inspect the event history, observer state, and the read models those observers produce.</sub>
</div>

<a id="cratis-cli"></a>

# Cratis CLI: inspect Chronicle and create projects

Cratis CLI gives you the `cratis` command to inspect and diagnose Cratis Chronicle, discover a running Cratis Arc application's commands and queries, and create projects from templates.

When a read model looks wrong, use the CLI to check recorded events and the observers that process them from the same terminal, including over SSH. Chronicle Workbench provides the browser alternative. Neither requires you to write an inspection application.

---

## Start here

- [Install and connect the CLI](#install)
- [Follow the installation and connection tutorial](https://www.cratis.io/cli/getting-started/)
- [Create a new application](https://www.cratis.io/cli/new/)
- [Read the CLI documentation](https://www.cratis.io/cli/)
- [Inspect Chronicle from the terminal](#use)
- [Browse releases and native binaries](https://github.com/Cratis/cli/releases)

## Place in the Cratis ecosystem

[Chronicle](https://github.com/Cratis/Chronicle) ([docs](https://www.cratis.io/chronicle/)) is Cratis's event-sourcing database and runtime. The CLI inspects its event history and processing state. For local browser inspection, Chronicle bundles [Workbench](https://www.cratis.io/chronicle/workbench/), which also lets authorized users try supported projection behavior. On macOS and Linux, you can install the CLI from the [homebrew-cratis](https://github.com/Cratis/homebrew-cratis) tap.

Start an investigation with `cratis chronicle diagnose`: it summarizes connection health, observers, failed partitions, and recommendations, then names commands for looking closer. The [CLI scenarios](https://www.cratis.io/cli/scenarios/) continue that investigation through a particular task. If you use a coding agent, the CLI can install [Cratis AI guidance](https://github.com/Cratis/AI) into its repository. You still need to review changes and check application behavior.

For a running [Arc](https://github.com/Cratis/Arc) ([docs](https://www.cratis.io/arc/)) application, use `cratis arc` to discover registered commands and queries through its HTTP catalogs. Arc is an independent CQRS framework: it separates commands that change state from queries that read it. This workflow does not require Chronicle.

Blog: [blog.cratis.io](https://blog.cratis.io)

The examples below describe human-operated commands and current output behavior, not an unversioned stable machine contract. Before automating them, check the output-format documentation for your CLI version. Re-create generated command context after upgrading.

## When the read model is wrong

In an event-sourced application, events record changes and read models hold the state your
application queries. Observers process those events. Projections and reducers build read models;
reactors run application behavior in response to events.

A stale read model can have several causes: the expected event was never appended, processing
is behind, or an observer failed. The read model alone cannot tell you which happened. Compare
the recorded events with observer state and failed partitions, the parts of an observer's work
that failed. Event history helps explain the state, but it only contains the facts the
application recorded.

Chronicle's browser Workbench provides an authenticated inspection path. If you are working in
a terminal over SSH, start with the CLI's diagnostic report instead:

```text
❯ cratis chronicle diagnose

── Chronicle Diagnostics  14:18:21 ─────────────────────────────────────────────
  server:      chronicle://chronicle-dev-client:***@localhost:35100/
  event store: Bookshop  /  Default

  ✓  Connection             connected
  ✓  Server version         16.7.0
  ✓  Event stores           2 stores: System, Bookshop
  ✓  Observers              9 active  0 quarantined
  ✓  Quarantined observers  0 quarantined (known count)
  ✗  Failed partitions      1 need attention  → cratis chronicle failed-partitions list
  ✓  Recommendations        none
  ✓  Event sequence         tail: 22
  !  Bookshop/Default: Failed partition: BookInventory/9780134757599

  ✗ Issues detected — review items above
```

The failed-partition row names the command that investigates that condition. `diagnose` exits
non-zero when the server is unreachable, failed partitions or quarantined observers exist, or any
check could not run. Suspended or disconnected observers, server updates, and pending
recommendations alone do not change the exit code.

<sub>Chronicle's default port is 35000; the throwaway server these recordings run against sits
on 35100 so it cannot collide with a real one.</sub>

## Install

<table>
<tr><td width="150"><b>Homebrew</b><br><sub>macOS · Linux</sub></td><td>

```bash
brew tap cratis/cratis
brew install cratis
```

</td></tr>
<tr><td><b>Binary</b><br><sub>no toolchain</sub></td><td>

```bash
V=$(curl -s https://api.github.com/repos/Cratis/cli/releases/latest | grep -m1 '"tag_name"' | cut -d'"' -f4)
curl -sSLo cratis.tar.gz "https://github.com/Cratis/cli/releases/download/$V/cratis-${V#v}-osx-arm64.tar.gz"
tar -xzf cratis.tar.gz && sudo mv cratis /usr/local/bin/
```

Release assets are named `cratis-<version>-<rid>.tar.gz` for `osx-arm64`, `osx-x64`,
`linux-arm64` and `linux-x64` — swap the last part. The version is in the filename, which is
why the URL is built from the tag rather than pointing at `latest/download`.

</td></tr>
<tr><td><b>.NET tool</b><br><sub>.NET 10+ runtime required</sub></td><td>

```bash
dotnet tool install -g Cratis.Cli
```

</td></tr>
<tr><td><b>Completions</b><br><sub>after installing</sub></td><td>

```bash
cratis completions install     # detects bash, zsh, fish or powershell
```

</td></tr>
</table>

> [!NOTE]
> Native release assets are self-contained and do not require a local .NET runtime. The
> `dotnet tool` package requires the runtime declared by the current package. Verify the exact
> release asset, runtime, and platform before installation.

Then point it at a server and check:

```bash
cratis context create dev --server chronicle://localhost:35000
cratis context set dev
cratis chronicle diagnose
```

Against a local Chronicle there is nothing to configure. The first run writes a `default`
context pointing at `chronicle://localhost:35000`, and the first `chronicle` command asks
which event store to make the default and remembers the answer.

## Use

Start with the inspection commands below. The same list includes state-changing commands such
as replay and reopening a stream. Before running those, confirm the server, event store,
namespace, target, and operational procedure; being able to inspect does not grant mutation
authority.

```bash
cratis chronicle diagnose                     # connection and Chronicle diagnostic summary
cratis chronicle diagnose --watch             # the same report, refreshing
cratis chronicle workbench                    # full-screen live dashboard

cratis chronicle event-stores list            # what stores exist on this server
cratis chronicle namespaces list              # namespaces inside the active store
cratis chronicle events get --from 100        # raw events off an event sequence
cratis chronicle events tail                  # the highest sequence number in use
cratis chronicle streams closed list          # closed scopes, origins, constraints and closing times
cratis chronicle streams closed reopen --event-source-id order-42 --reason "Closed the wrong order"  # repair an exact manual closure
cratis chronicle event-types list             # registered types, with generations
cratis chronicle event-types show <id>        # the JSON Schema for one

cratis chronicle observers list               # every observer, its state and its position
cratis chronicle observers show <id>          # one observer in detail
cratis chronicle observers replay <id>        # reprocess from sequence zero
cratis chronicle failed-partitions list       # partitions that have stopped
cratis chronicle failed-partitions show <observer> <partition>   # the exception, per attempt
cratis chronicle observers retry-partition <observer> <partition>
cratis chronicle observers clear-partition-quarantine <observer> <partition>   # release a quarantined partition and retry

cratis chronicle projections list             # projection declarations
cratis chronicle read-models list             # read model definitions
cratis chronicle read-models instances <name> # the projected state itself
cratis chronicle jobs list                    # replays, migrations, retries

cratis context list                           # configured servers
cratis init                                   # write CLI context files for configured tools
```

`--help` works on every group and every command. `cratis llm-context` prints the current
command catalog as JSON. Each command in it states its `effect` (`read-only`, `local`,
`mutating` or `destructive`) and whether it `requiresConfirmation`, so an agent can tell
observing commands from state-changing ones without its own list.

### Output formats

`-o` takes `table`, `plain`, `json` or `json-compact`. Plain output is tab-separated;
the JSON formats retain named structure. `-q` prints identifiers only for bounded read-only
selection or inspection:

```bash
cratis chronicle observers list -q | head -n 5
```

Select an explicit format for automation and bind parsing to the exact CLI version. These current
formats are not declared as an unversioned stable machine contract.

## Reading an observer row

Use an observer row to distinguish how far processing has read from the last event it handled.
Those positions can differ even when the observer is caught up:

```text
Id                       Type        State   Quarantined  Next#  LastHandled#  Subscribed
Bookshop.Members         Reducer     Active  False        23     2             False
Bookshop.Books           Reducer     Active  False        23     10            False
Bookshop.BorrowedBooks   Projection  Active  False        23     18            False
Bookshop.OverdueBooks    Projection  Active  False        23     22            False
Bookshop.OverdueNotices  Reactor     Active  False        23     22            False
```

| Column | What it means |
|---|---|
| `Next#` | the next sequence number this observer will look at |
| `LastHandled#` | the last event it actually processed |
| `State` | `Active`, `Replaying`, `Suspended`, `Disconnected`, `Quarantined` or `Unknown` |
| `Subscribed` | whether a client is currently attached to it |

> [!NOTE]
> **`LastHandled#` lagging the tail is normal, and is not the same thing as being behind.**
> Every observer above has `Next#` 23 against a tail of 22 — all of them are caught up. But
> `Members` last handled sequence 2, because no member has registered since; nothing between 3
> and 22 was addressed to it. The two columns answer different questions: `Next#` is how far it
> has read, `LastHandled#` is the last thing it cared about.
>
> This is why `diagnose` reports failed partitions rather than sequence lag. Lag is ambiguous.
> A failed partition is not.

`Disconnected` means no client is attached — usually the application is not running. It is the
normal state for a store whose application is stopped, and it is not an error.

## Following a failure to the event that caused it

A partition is one event source's slice of an observer. The CLI lists recorded failed
partitions and `failed-partitions show` prints the attempts available for the selected
observer and partition:

<div align="center">
<img src="https://raw.githubusercontent.com/Cratis/cli/main/assets/triage.gif" alt="The CLI listing a failed partition and the exception recorded for its processing attempt" width="860">
</div>

<sub>The diagnostic summary names the next command. The list names the observer and partition,
and the example partition turns out to be a book —
`978-0131177055` — whose overdue notice could not be sent.</sub>

This application's partition is an ISBN, the event source id for a book. The observer and
partition identify the work that failed, while the recorded attempts show what happened during
processing:

```text
FailedPartition: caadc869-1251-41d0-9063-6947eaf74043
Observer:        Bookshop.OverdueNotices
Partition:       978-0131177055
Attempts:        5

  --- Attempt at 2026-07-28T12:17:56.6680000+00:00 (Seq# 22) ---
  Exception has been thrown by the target of an invocation.
  smtp.bookshop.local: connection refused
```

The example records several processing attempts. After fixing the underlying cause, use
`retry-partition` to request another attempt for the exact observer and partition.

```bash
cratis chronicle observers retry-partition Bookshop.OverdueNotices 978-0131177055 -y
```

> [!WARNING]
> `observers replay <id>` is the bigger hammer: it reprocesses that observer from sequence
> zero and rebuilds its read model. On a large store it is neither instant nor free. Confirm
> the exact event store, namespace, observer, and operational procedure before running it.
> Reach for `retry-partition` first when one failed partition is the intended scope.

A partition that exhausted its retry attempts is quarantined, and `retry-partition` refuses it.
`observers clear-quarantine` does not help: it clears the observer only. Use
`observers clear-partition-quarantine <observer> <partition>` (Chronicle 19.29.0 or later), which
resets the retry budget and starts the retry. The retry re-runs the handler, so side effects that
already happened will happen again.

## The terminal workbench

`cratis chronicle workbench` opens the CLI's full-screen terminal dashboard over the same connection: fifteen
views, refreshing on an interval, with the actions available in place — `R` replays the
selected observer, `T` retries a failed partition, `S` and `U` stop and resume jobs.

`F` filters whatever view you are on, and it reopens on the view you left it on.

`Ctrl+P` is the part worth knowing about. It searches five current artifact kinds at once:

<div align="center">
<img src="https://raw.githubusercontent.com/Cratis/cli/main/assets/workbench.gif" alt="Filtering the Workbench observers view to one application, then searching observers, event types, projections, read models, and failed partitions from the command palette" width="860">
</div>

<sub>`F` narrows the view to one application. `Ctrl+P` then matches a single word across five
kinds at once — the reactor, the projection's observer, the event type they both read, the
projection declaration and the read model it writes. Picking one jumps to its view with the
filter already applied.</sub>

Search for "Overdue" when you need to follow the same feature across its event types, handlers,
and read model. Selecting a match opens the relevant view with the filter applied, so you can
continue without finding each name separately.

| Key | |
|---|---|
| `F` | filter the current view |
| `Ctrl+P` | search observers, event types, projections, read models, and failed partitions |
| `↑ ↓` | move within the sidebar or the table |
| `← →` | put focus on the sidebar / on the content |
| `Home` / `Shift+G` | first row / last row |
| `[` `]` | previous / next page |
| `R` | replay the selected observer |
| `T` / `P` | retry / replay the selected failed partition |
| `S` / `U` | stop / resume the selected job |
| `A` / `I` | apply / ignore the selected recommendation |
| `D` / `V` | event type definition / the observers that read it |
| `Enter` | open the read model detail (Read Models view) |
| `Ctrl+B` | collapse the sidebar |
| `Ctrl+\` | toggle the detail pane |
| `Ctrl+E` / `Ctrl+N` | switch event store / namespace |
| `Ctrl+C` | copy the open detail |
| `F9` `F10` `F11` | themes |
| `?` | help |
| `Q` | quit |

Mutation commands use a confirmation dialog rather than a status-bar prompt. Recheck the
selected event store, namespace, target, and current state before confirming any action.

## Output selection and generated context

The CLI selects a default output format from process context. This is a heuristic convenience,
not proof of who or what reads the output:

| Process context | Default format |
|---|---|
| interactive terminal | `table` |
| redirected output | `json` |
| `NO_COLOR` set | `plain` |
| recognized tool-environment marker | `json-compact` |

Use `-o` to select an explicit current format. Before building automation, review the
[current output-format section](#output-formats) and the exact CLI
version you deploy; ordinary output formats are not declared as an unversioned stable contract.

The CLI can also write a snapshot of its current command surface for configured tools:

```bash
cratis init            # write context files for detected/configured tools
cratis init --refresh  # replace the snapshot after upgrading the CLI
cratis llm-context     # print the current command catalog as JSON
```

The generated context is a snapshot. Refresh it after upgrading the CLI. If an instruction file
is generated from another source, use `--no-context` and update that source rather than editing
the generated instruction file directly.

## Tab completion asks the server

`cratis completions install` writes a completion script for bash, zsh, fish or PowerShell.
It is not a static word list:

<div align="center">
<img src="https://raw.githubusercontent.com/Cratis/cli/main/assets/completions.gif" alt="pressing tab after cratis chronicle read-models instances and getting the read model names registered on the live server" width="860">
</div>

<sub>Completing a read model name shells back into the CLI, which connects and returns what that
server has registered right now — then the completed command runs against it.</sub>

Observers, event stores, event types, projections, read models, jobs, recommendations,
subscriptions, applications and users all complete this way — and context names, which come
from your config rather than the server. Completion failures are swallowed and return nothing,
so a server that is down costs you a tab press rather than a broken shell.

## Contexts

A context is a named server profile. `cratis context create staging --server …` then
`cratis context set staging`, and every subsequent command follows it.

The connection string is resolved in a fixed order, first match winning:

| | |
|---|---|
| 1 | `--server` on the command |
| 2 | `CHRONICLE_CONNECTION_STRING` |
| 3 | the active context in `~/.cratis/config.json` |
| 4 | `chronicle://localhost:35000` |

<details>
<summary>Credentials and connection strings</summary>

<br>

Connection strings and context files can contain credentials. Treat them as secrets, avoid
placing them in command history, public issues, or shared logs, and inspect every command output
before sharing it.

</details>

<details>
<summary>Event store and namespace</summary>

<br>

`-e/--event-store` and `-n/--namespace` follow the same shape, defaulting to the context and
then to `default` / `Default`. The first `chronicle` command against a server whose event
store is unknown asks which one to use and remembers the answer; `cratis context set-value
event-store <name>` changes it later.

</details>

## Creating projects

To start an application, `cratis new` creates project files from the published [Cratis templates](https://github.com/Cratis/Templates). Creating those files does not require the .NET SDK: the CLI's own template engine reads the `template.json` format used by `dotnet new`, acquires the package from nuget.org, and runs its finishing steps. These include resolving `Version="*"` package references to concrete published versions. Building and running the application still requires its language toolchain.

```bash
# Creation wizard: which template, which language,
# which database — defaults on Enter
cratis new

# List the available templates with their languages and databases
cratis new list

# Scaffold the full-stack web application in C#, Kotlin, or Java
# (--language is required when instantiating a template)
cratis new cratis --language csharp -n MyApp -o MyApp
cratis new cratis --language kotlin -n MyKotlinApp -o MyKotlinApp
cratis new cratis --language java -n MyJavaApp -o MyJavaApp

# Inspect a template's own parameters first
cratis new cratis --language csharp --parameters

# Other options
cratis new cratis --language csharp -n MyApp --dry-run  # report what would be created, write nothing
cratis new cratis --language csharp -n MyApp --Framework net10.0   # template parameters as --<Name> <value>
cratis new cratis --language csharp -n MyApp --allow-scripts no   # explicit policy for script post actions
cratis new cratis --language csharp -n MyApp --database postgresql # database backend (mongodb, postgresql, mssql, sqlite)
```

The command follows `dotnet new` semantics: `-n` names the project and `-o` selects its output directory. Here, unlike other CLI commands, `-o` does not select an output format. Its template store is separate from `dotnet new`'s; `dotnet new install Cratis.Templates` remains an alternative way to install the same templates. The `dotnet restore` finishing step runs when dotnet is on your PATH. Otherwise, the summary tells you how to run it later.

The templates are installed under `~/.cratis/templates`: the first `cratis new` downloads the latest set from NuGet, later runs use it offline, and `cratis update` installs newer templates along with the CLI (`--no-templates` skips that).

See the [creating projects documentation](https://www.cratis.io/cli/new/) for the template catalogue, parameters, exit codes, and the `--allow-scripts` contract.

## Cratis AI

`cratis ai` installs Cratis-owned AI guidance — rules, skills, and harness integration for coding agents — into a repository. The selection is recorded in the project-owned `.cratis/ai.json`; the [Cratis `dotnet new` templates](https://github.com/Cratis/Templates) ship it preconfigured. After creation, `cratis new` automatically attempts to synchronize that selection and reports failures or conflicts without discarding the scaffold. Inside the created project, use this command to refresh guidance later or retry after resolving a setup failure:

```bash
cratis ai update
```

```bash
cratis ai install                        # select harnesses, profiles, and languages
cratis ai update                         # synchronize the configured selection
cratis ai update --dry-run               # report what would change, write nothing
cratis ai status                         # installed revision, update availability, conflicts
cratis ai profiles list                  # profiles the corpus offers
cratis ai uninstall                      # remove unchanged Cratis-managed content
```

Update only touches Cratis-managed files — never yours. See the [Cratis AI documentation](https://www.cratis.io/cli/ai/) for the configuration format and the full command reference.

## Other command groups

Choose these groups for tasks outside Chronicle inspection. Follow each group's reference for its supported behavior and limits; a command in the tree does not establish product completeness, support, compatibility, or availability.

- `cratis arc` inspects registered commands and queries in a running [Arc](https://github.com/Cratis/Arc) application.
- `cratis view` opens a .NET application as an event model in the browser, from the Screenplay documents it embeds or ones generated from its source. See the [View command reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/view.md).
- `cratis build` connects a machine to [Cratis Studio](https://cratis.studio) as a builder and builds slices with Claude Code, an agent over the Agent Client Protocol, or a generator; installs build kits; and connects a local agent to a board for modeling. `cratis studio fetch` fetches the context of a slice, feature, event model or domain for an agent. See the [Build command reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/build.md).

### Work with Screenplay files

`cratis screenplay` and `cratis render` work with Cratis Screenplay (`.play`) documents: generate, validate, and render from files, without a running application.

The CLI bundles Screenplay 4.250.0 and Stage 4.201.1; rendering admits ESM v1 through v32, including Uuid equality and inequality in `require` and `produces when`. `cratis run` uses Cratis Stage's local Docker sandbox to run a supported subset of Screenplay, including ESM v27 composite read-model keys on the `semantic` engine (the default); the deprecated `eventmodel` engine refuses them, logs a startup warning and will be removed in Stage v6. At Stage 4.154.1 `cratis render` reports composite read-model keys with `STAGE-ESM-048` (read models with two or more key parts and projections keyed by `parts`) and `STAGE-ESM-047` (single-instance queries by key parts); ESM v33 and newer are refused with `CLI-RENDER-004`.

Rendering also covers resolved C# command and concept rule bodies returning `bool` or yielding validation messages, validation rules on generated concepts and v18 query rejection specifications, event sources, streams, routed commands and policy negation, and carries authored `description` and event `documentation` into XML doc comments. ESM v16 keeps an event's stored type name through its `id` pin when you rename it. Every render checks existing event type ids before publication and refuses changes or unreadable ids with `CLI-RENDER-016` and `STAGE-EVENT-002`.

Server-generated values render with v15 concept generation strategies and freshness scopes, snapshot collection queries render as one-time lists, observable collection queries return Chronicle's live stream through Arc, equality filters render on both, and scalar or named command responses render.

Projection `all` blocks with event-source identity mappings render as Chronicle `FromAll`; literal-mapping or mapping-less blocks still report `STAGE-ESM-017`. PascalCase event-source names render as `{Name}EventSource` with public stream-name constants; colliding constants report `STAGE-ESM-012`; unsupported generated types report `STAGE-ESM-028` and unsupported generation/response combinations `STAGE-ESM-038`; specifications whose unauthenticated callers carry roles or claims are refused with `STAGE-ESM-011`. See the [Screenplay command reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/screenplay.md) for the supported constructs and other renderer limits.

### Compare a model with recorded behavior

`cratis screenplay conform-namespace <MODEL_ROOT> -e <STORE> -n <NAMESPACE>` compares an authored model with what a Chronicle namespace holds: registered event types, appends attributed to commands and reactors, and reactor subscriptions. It reads event metadata only, up to a captured tail and `--max-events` (default 100000), and reports a reached bound as partial coverage. See [namespace conformance](https://github.com/Cratis/cli/blob/main/Documentation/reference/screenplay.md#cratis-screenplay-conform-namespace-model_root).

### Install local Screenplay desktop tooling

`cratis screenplay desktop install --clients claude,chatgpt` acquires verified, self-contained local Screenplay desktop packages. Complete installation/enabling in each host's UI; use `status`, `update`, and `uninstall` to manage Cratis-owned sources safely. See [desktop MCP installation](https://github.com/Cratis/cli/blob/main/Documentation/reference/screenplay-desktop-mcp.md).

### Create, restore, update and vendor extensions

`cratis extension new` creates a full extension (an OCI artifact with an optional C#, Kotlin, Go or TypeScript backend and an optional React, mount or Screenplay-authored frontend) or a frontend-only npm component package, with a wizard or flags, from the `Cratis.Templates.Extensions` templates; see [create an extension package](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-new.md). `cratis extension restore` fills the extension cache from `screenplay.lock` (`--locked` in CI never changes the lock), `cratis extension update [SYSTEM]` moves references to the newest release in their range and reports how each contract changes and which uses in the model it breaks, and `cratis extension vendor <DESTINATION>` copies the locked packages, with their signatures, to a folder or a private registry for air-gapped deployments. Packages are checked against the publisher allow-list in `.cratis/extensions.json`; registry credentials come from `cratis config`. See the [extension reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md).

### Read release history

`cratis release-notes` shows what changed in each release, offline, one release at a time; `cratis update` shows the releases it skipped after updating. See the [Release notes reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/release-notes.md).

For the documented command groups and their workflows, return to the [canonical CLI page](https://www.cratis.io/cli/).

## Platforms

| | macOS | Linux | Windows |
|---|---|---|---|
| Homebrew | Available | Available | Not available |
| Native binary | `osx-arm64`, `osx-x64` | `linux-arm64`, `linux-x64` | Not available |
| .NET global tool | Package available | Package available | Package available |
| Completion script | bash, zsh, fish | bash, zsh, fish | PowerShell |

> [!NOTE]
> **There is no native Windows binary.** Current native release assets cover macOS and Linux on
> arm64 and x64. On Windows, install the .NET global tool. The repository's current CI exercises
> Ubuntu and macOS; verify Windows behavior for the exact version and workflow you adopt.

## Development

```bash
dotnet build -c Release
dotnet test -c Release
./install.sh                   # pack and install the local build as a global tool
```

Integration specs run the CLI against a real Chronicle server in a container, so Docker has to
be running for those.

Specs follow the convention used across the Cratis codebases — `for_<subject>` names what is
under specification, `when_<scenario>` names the situation, and each `should_<expectation>`
observes one thing, so a failure reads as a sentence.

### The GIFs

They are scripted, not screen-captured, and re-render from a clean checkout:

```bash
assets/record.sh               # every clip: sets up the store, waits for it, renders
assets/record.sh workbench     # or just one
```

[Recording notes](https://github.com/Cratis/cli/blob/main/assets/RECORDING.md) covers how they were made, what the fixture
contains and why each clip earns its place.

### Releasing

Repository release behavior is path- and label-sensitive. A documentation-only pull request uses
the `no-release` label and does not publish packages or native assets. Source/package changes use
the release-impact label required by the repository policy; review the pull request's declared
effects before merge.

## Community and repository

| | |
|---|---|
| Questions and help | [Discord](https://discord.gg/kt4AMpV8WV) |
| 🐛 | [Issues](https://github.com/Cratis/cli/issues) |
| 🔒 | [Private security reporting](mailto:oss@cratis.io?subject=Security%3A) |
| 📚 | [CLI documentation](https://www.cratis.io/cli/) |

## License

MIT. See the [repository license](https://github.com/Cratis/cli/blob/main/LICENSE).

Release notes and announcements: the [Cratis blog](https://blog.cratis.io).
