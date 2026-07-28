<div align="center">
  <a href="https://cratis.io">
    <img src="https://raw.githubusercontent.com/Cratis/cli/main/cratis.svg" alt="Cratis" width="420" style="background-color: white">
  </a>

  <h3 align="center">Cratis CLI</h3>

  <p align="center"><b>A terminal window into a running Chronicle event store — and into why its read models are wrong.</b></p>

  <p align="center">
    <a href="https://www.nuget.org/packages/Cratis.Cli"><img src="https://img.shields.io/nuget/v/Cratis.Cli?logo=nuget" alt="NuGet"></a>
    <a href="https://github.com/Cratis/cli/releases/latest"><img src="https://img.shields.io/github/v/release/Cratis/cli?color=86efac" alt="Release"></a>
    <a href="#platforms"><img src="https://img.shields.io/badge/platform-macOS%20%7C%20Linux%20%7C%20Windows-lightgrey" alt="Platform"></a>
    <a href="https://discord.gg/kt4AMpV8WV"><img src="https://img.shields.io/discord/1182595891576717413?label=Discord&logo=discord&color=7289da" alt="Discord"></a>
    <a href="#license"><img src="https://img.shields.io/badge/license-MIT-green" alt="License"></a>
  </p>

  <img src="assets/demo.gif" alt="cratis diagnose reporting a healthy store, then the raw events on the log, then the read model those events produced" width="900">

  <sub>Is it healthy, what happened, and what state did that produce —<br>
  the three questions, in the order you actually ask them.</sub>
</div>

---

## When the read model is wrong

An event-sourced system splits two things that used to live together: **what the state is**,
and **why it is that way**. Events append to a log. Observers — projections, reducers, reactors
— consume that log and derive the read models your application queries.

That split is the point. It is also what makes debugging strange, because when a read model
looks wrong the answer is in neither place on its own. The database holds the derived state but
not the reason. The log holds the reason but not the derived state. And in between sits an
observer that may be working, may be behind, or may have stopped four events ago — and **an
observer that has stopped consuming looks exactly like an observer with nothing to do.**

Chronicle ships a browser Workbench that answers this. But it is served by the server, behind
the server's auth, and you are on a box you reached over SSH.

`cratis` is that view from a terminal:

```
❯ cratis chronicle diagnose

── Chronicle Diagnostics  14:18:21 ─────────────────────────────────────────────
  server:      chronicle://chronicle-dev-client:***@localhost:35100/
  event store: Bookshop  /  Default

  ✓  Connection            connected
  ✓  Server version        16.7.0
  ✓  Event stores          2 stores: System, Bookshop
  ✓  Observers             9 active
  ✗  Failed partitions     1 need attention  → cratis chronicle failed-partitions list
  ✓  Recommendations       none
  ✓  Event sequence        tail: 22

  ✗ Issues detected — review items above
```

Every failing line prints the command that investigates it. `diagnose` exits non-zero when it
finds something, so it also works as a health check in a script.

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
<tr><td><b>.NET tool</b><br><sub>any platform, .NET 10+</sub></td><td>

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
> The native binaries are self-contained — about 38 MB compressed, 104 MB on disk — so they
> need no .NET installed. The `dotnet tool` package is a fraction of that and needs the
> runtime you already have.

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

```bash
cratis chronicle diagnose                     # whole-server verdict, non-zero exit when unhealthy
cratis chronicle diagnose --watch             # the same report, refreshing
cratis chronicle workbench                    # full-screen live dashboard

cratis chronicle event-stores list            # what stores exist on this server
cratis chronicle namespaces list              # namespaces inside the active store
cratis chronicle events get --from 100        # raw events off an event sequence
cratis chronicle events tail                  # the highest sequence number in use
cratis chronicle event-types list             # registered types, with generations
cratis chronicle event-types show <id>        # the JSON Schema for one

cratis chronicle observers list               # every observer, its state and its position
cratis chronicle observers show <id>          # one observer in detail
cratis chronicle observers replay <id>        # reprocess from sequence zero
cratis chronicle failed-partitions list       # partitions that have stopped
cratis chronicle failed-partitions show <observer> <partition>   # the exception, per attempt
cratis chronicle observers retry-partition <observer> <partition>

cratis chronicle projections list             # projection declarations
cratis chronicle read-models list             # read model definitions
cratis chronicle read-models instances <name> # the projected state itself
cratis chronicle jobs list                    # replays, migrations, retries

cratis context list                           # configured servers
cratis init                                   # teach this project's AI tools about the store
```

`--help` works on every group and every command. `cratis llm-context` prints the whole
surface as JSON.

### Output formats

`-o` takes `table`, `plain`, `json` or `json-compact`, and `-q` prints identifiers only.
Measured on one store of 23 events and 9 observers:

| | `events get` | `observers list` | `event-types list` |
|---|---|---|---|
| `-o plain` | 1,615 B | 777 B | 561 B |
| `-o json-compact` | 7,329 B | 1,684 B | 1,656 B |
| `-o json` | 8,981 B | 2,093 B | 2,505 B |
| `-q` | 59 B | 403 B | 310 B |

`plain` is tab-separated and 2.7× to 5.6× smaller than `json` on this store — widest on
`events get`, where JSON repeats four field names across every one of 23 rows. `-q` exists to
be piped:

```bash
cratis chronicle observers list -q | xargs -I {} cratis chronicle observers replay {} -y
```

## Reading an observer row

This is the table you will spend the most time in, and two of its columns are easy to
misread:

```
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

A partition is one event source's slice of an observer. When processing throws, Chronicle
stops **that partition** and leaves the rest of the observer running, so one bad entity does
not halt everything. `failed-partitions show` prints what happened, per attempt:

<div align="center">
<img src="assets/triage.gif" alt="diagnose flagging one failed partition in an otherwise healthy store, listing it, then printing the exception that stopped it" width="860">
</div>

<sub>Nine observers running, one partition stopped. The verdict names the next command, the
list names the observer and the partition, and the partition turns out to be a book —
`978-0131177055` — whose overdue notice could not be sent.</sub>

The partition is the ISBN because that is the event source id this application uses. Whatever
your entities are keyed by is what you will see here, which is what makes the failure
addressable rather than merely reported:

```
FailedPartition: caadc869-1251-41d0-9063-6947eaf74043
Observer:        Bookshop.OverdueNotices
Partition:       978-0131177055
Attempts:        5

  --- Attempt at 2026-07-28T12:17:56.6680000+00:00 (Seq# 22) ---
  Exception has been thrown by the target of an invocation.
  smtp.bookshop.local: connection refused
```

The gaps between attempts widen — 2 seconds, then 4, then 8. Chronicle backs off and keeps
retrying on its own, so a partition that failed on something transient recovers without you.
`retry-partition` exists for the other case: you have fixed the cause and do not want to wait
for the next attempt.

```bash
cratis chronicle observers retry-partition Bookshop.OverdueNotices 978-0131177055 -y
```

> [!WARNING]
> `observers replay <id>` is the bigger hammer: it reprocesses that observer from sequence
> zero and rebuilds its read model. Nothing is lost — events are immutable and replay is what
> they are for — but on a large store it is neither instant nor free. Reach for
> `retry-partition` first; it reprocesses one partition rather than the whole log.

## The workbench

`cratis chronicle workbench` opens a full-screen dashboard over the same connection: fifteen
views, refreshing on an interval, with the actions available in place — `R` replays the
selected observer, `T` retries a failed partition, `S` and `U` stop and resume jobs.

`F` filters whatever view you are on, and it reopens on the view you left it on.

`Ctrl+P` is the part worth knowing about. It searches **every kind of artifact at once**:

<div align="center">
<img src="assets/workbench.gif" alt="filtering the workbench observers view down to one application, then searching every artifact kind at once from the command palette" width="860">
</div>

<sub>`F` narrows the view to one application. `Ctrl+P` then matches a single word across five
kinds at once — the reactor, the projection's observer, the event type they both read, the
projection declaration and the read model it writes. Picking one jumps to its view with the
filter already applied.</sub>

That breadth is the reason it is a palette and not a search box. "Overdue" is not a name you
look up in one list; it is a thread running through five of them, and following it is what
you were actually doing.

| Key | |
|---|---|
| `F` | filter the current view |
| `Ctrl+P` | search every artifact kind |
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

Destructive actions go through a confirmation dialog rather than a status-bar prompt, so a
refresh landing mid-question cannot leave you answering something that is no longer on
screen.

## The interesting part: it knows when a machine is reading it

Run `cratis chronicle observers list` in a terminal and you get a bordered table. Pipe it into
a file and you get JSON. Run it from inside Claude Code, Cursor or Windsurf and you get
compact JSON, with no banner and no update notice.

Nothing about the command changed. The CLI resolved the format from its surroundings:

| Surroundings | Format | Why |
|---|---|---|
| a terminal | `table` | a person is reading it |
| output redirected | `json` | something is parsing it |
| `NO_COLOR` set | `plain` | [an explicit request](https://no-color.org/) for no decoration |
| an agent environment | `json-compact` | a model is reading it, and pays per token |

The last row is the one that needed a decision. Agents are a real caller now, and they had
been getting the human output — tables drawn with box-drawing characters, spent on a reader
that cannot see them. Detection is a handful of environment variables (`CLAUDECODE`,
`CURSOR_TRACE_DIR`, `WINDSURF_SESSION_ID` and friends), and it suppresses the banner and the
update hint too, because a startup notice in an agent's transcript is noise it has to reason
about. `CRATIS_NO_UPDATE_CHECK=1` turns that check off for everyone else — a cron job's log is
not the place to be told about a new release either.

Compact JSON rather than `plain` is deliberate even though `plain` is smaller. Tab-separated
output loses nesting and asks the reader to remember what column four was; JSON names every
field. Where `plain` wins by a lot, the machine-readable command catalog says so explicitly
rather than making it the default.

That catalog is the other half:

```bash
cratis init          # writes CHRONICLE.md, wires up Claude / Copilot / Cursor / Windsurf
cratis llm-context   # the whole command surface as JSON, ~50 KB
```

`init` detects which tools a project already uses rather than assuming, and `llm-context`
carries per-command descriptions, options, examples and output-format advice — so an agent
can work out that a stuck observer means `failed-partitions show` without being told.

## Tab completion asks the server

`cratis completions install` writes a completion script for bash, zsh, fish or PowerShell.
It is not a static word list:

<div align="center">
<img src="assets/completions.gif" alt="pressing tab after cratis chronicle read-models instances and getting the read model names registered on the live server" width="860">
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
<summary>Credentials, and why they are composed in separately</summary>

<br>

Credentials are not part of that chain. Whatever connection string wins, the CLI then checks
whether it already carries authentication — embedded `user:pass@`, or an `apiKey=` parameter
— and leaves it alone if so. Otherwise it composes in, in order: a cached token from
`cratis chronicle login`, then the context's client id and secret, then Chronicle's
well-known development credentials.

Keeping the two separate is what makes `--server` useful. You can point a command at a
different host for one invocation without restating the credentials, and a connection string
you paste from somewhere else is used exactly as given rather than being quietly rewritten.

Connection strings are redacted to `user:***@host` wherever the CLI prints them.

</details>

<details>
<summary>Event store and namespace</summary>

<br>

`-e/--event-store` and `-n/--namespace` follow the same shape, defaulting to the context and
then to `default` / `Default`. The first `chronicle` command against a server whose event
store is unknown asks which one to use and remembers the answer; `cratis context set-value
event-store <name>` changes it later.

</details>

## Beyond Chronicle

<details>
<summary><b><code>cratis arc</code></b> — introspect a running Cratis Arc application</summary>

<br>

```bash
cratis arc commands list --url http://localhost:5000
cratis arc queries list
```

Lists the command and query endpoints an Arc application has registered, with routes,
namespaces and documentation. Talks HTTP to the application rather than gRPC to Chronicle, so
`--url` and `ARC_URL` apply instead of `--server`.

</details>

<details>
<summary><b><code>cratis prologue</code></b> — turn a running system into a Screenplay</summary>

<br>

```bash
cratis prologue start          # wizard, writes cratis-prologue.json
cratis prologue interpret      # reads the captures, writes a .play
```

[Prologue](https://github.com/Cratis/Prologue) observes an ordinary system — HTTP commands,
database changes, OpenTelemetry — and `interpret` turns those captures into a Cratis
Screenplay. Heuristics build the structure; a language model configured through
`cratis llm use` refines the naming and asks about the decisions it is unsure of.

</details>

<details>
<summary><b><code>cratis run</code></b> — boot a Screenplay in a local sandbox</summary>

<br>

```bash
cratis run                     # every .play in this folder
cratis run ./screenplays --port 9191
```

Runs the `.play` files in a folder in a local Stage container. Needs Docker on the `PATH`.

</details>

<details>
<summary><b><code>cratis llm</code></b> — configure the model those tools use</summary>

<br>

```bash
cratis llm use anthropic --api-key …
cratis llm show                # api keys are masked
cratis llm clear
```

Stored in the `llm` section of `~/.cratis/config.json`. `anthropic`, `openai` and `local`
(any OpenAI-compatible endpoint) are supported.

</details>

## Platforms

| | macOS | Linux | Windows |
|---|---|---|---|
| Homebrew | ✅ | ✅ | — |
| Native binary | `osx-arm64`, `osx-x64` | `linux-arm64`, `linux-x64` | — |
| .NET global tool | ✅ | ✅ | ✅ |
| Completion script | bash, zsh, fish | bash, zsh, fish | PowerShell |

> [!NOTE]
> **There is no native Windows binary.** A release publishes four: macOS and Linux, arm64 and
> x64. Windows goes through `dotnet tool install -g Cratis.Cli`, which needs .NET 10 —
> nothing in the CLI is platform-specific, but CI builds and tests on Ubuntu and macOS only,
> so Windows is untested rather than unsupported.

## Development

```bash
dotnet build -c Release        # 5 projects, zero warnings
dotnet test -c Release         # 237 unit specs, 161 integration specs
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

[`assets/RECORDING.md`](assets/RECORDING.md) covers how they were made, what the fixture
contains and why each clip earns its place.

### Releasing

There is no version to bump. Label a pull request `major`, `minor` or `patch` and merging it
cuts the release: [cratis/release-action](https://github.com/cratis/release-action) works out
the next semantic version and tags it, and the workflows publish the NuGet package, build the
four native binaries and push the Homebrew formula. A merge with none of those labels releases
nothing.

## Support

| | |
|---|---|
| 💬 | [Discord](https://discord.gg/kt4AMpV8WV) |
| 🐛 | [Issues](https://github.com/Cratis/cli/issues) |
| 📚 | [cratis.io](https://cratis.io) |

## License

MIT. See [`LICENSE`](./LICENSE).
