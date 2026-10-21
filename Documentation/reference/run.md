---
description: Boot a local Stage sandbox from a Screenplay file or folder, with a Chronicle kernel and in-memory event store, using cratis run.
---

# Run

`cratis run` boots a local [Stage](https://github.com/Cratis/Stage) sandbox from a Screenplay (`.play`) file or a folder of them. It packages a Chronicle kernel, the Stage engine, and an in-memory event store into a single throwaway container so you can play with an event model straight from its `.play` source — no server to set up, nothing to clean up afterward.

```bash
cratis run [PATH]
```

```text
  Running the Screenplay from /work/invoicing

  Stage API           http://localhost:9090
  API reference       http://localhost:9090/scalar/v1
  Chronicle Workbench https://localhost:35000
                      HTTPS only — sign in with admin / ChangeMeNow!

  ✓ Ready — event model 'Invoicing' as event store 'gentle-zephyr'
  Runtime engine      semantic
  Press Ctrl+C to stop
```

You point it at a folder of `.play` files (or run it from inside one), or at a single `.play` file, and it hands exactly that to the Stage container, read-only. The Stage compiles the file, or every `.play` file in the folder as one application, and exposes a live API you can drive — plus the Chronicle Workbench, for looking at what the model does to the event store.

```bash
cratis run ./screenplays
cratis run "./screenplays/invoicing model.play"
```

The container's own output is kept out of the way. While it boots you get a
progress line — pulling the image, starting Chronicle, compiling the Screenplay
files, registering read models — and `Ready` appears once the Stage reports
that the model is ready to run. A model the selected engine cannot execute
is reported as a refusal instead of a ready session.

## Prerequisites

- **Docker** must be installed and the `docker` command on your `PATH`. `cratis run` shells out to `docker run`.
- The `cratis/stage` image is pulled automatically on first use.

## Arguments

| Argument | Description |
|---|---|
| `PATH` | Screenplay (`.play`) file, or folder of Screenplay files, to run. A folder is searched recursively. The `.play` extension matches in any casing. Defaults to the current directory. |

## Options

| Option | Description |
|---|---|
| `--tag <TAG>` | The `cratis/stage` image tag to run. Default: `4.190.7`, the Stage version this CLI renders with. |
| `--engine <ENGINE>` | Runtime engine: `semantic` (default) or `eventmodel` (deprecated explicit fallback), case-insensitive. A refusal never switches engines. See [Runtime engines](#runtime-engines). |
| `--port <PORT>` | Host port to publish the Stage API on. Default: `9090`. |
| `--workbench-port <PORT>` | Host port to publish the Chronicle Workbench on. Default: `35000`. |
| `--package-set <FILE>` | Approved package set (JSON) naming the custom runtime packages the `ui profile` may use, beyond `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default`. The Stage loads them at runtime. See [Custom packages](#custom-packages). |
| `--packages <FOLDER>` | Folder holding a folder for each approved package, named by its npm name (for example `./node_modules`). Default: the folder the package set is in. Needs `--package-set`. |
| `--verbose` | Stream the container's output instead of showing startup progress, and list every runtime refusal after its grouped summary. JSON formats keep container output captured. |

`cratis run` runs ESM v27 composite read-model keys and ESM v28 Uuid equality and inequality on the `semantic` engine (the default). The deprecated `eventmodel` engine refuses both; Uuid comparisons report `STAGE-ESM-049`.

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## What it does

For a folder, the command mounts it read-only into the Stage container and publishes both of the container's ports to your host (replace `<STAGE_TAG>` with the Stage version bundled with your CLI):

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel:ro -e Stage__Runtime__Engine=semantic cratis/stage:<STAGE_TAG>
```

For a single file, it mounts only that file, read-only, at a fixed path and passes that path to the Stage after the image:

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD/invoicing.play":/eventmodel/input.play:ro -e Stage__Runtime__Engine=semantic cratis/stage:<STAGE_TAG> /eventmodel/input.play
```

- A folder is mounted at `/eventmodel` inside the container; Stage finds every `.play` file beneath it and compiles them as one application.
- A file is mounted at `/eventmodel/input.play`, and Stage compiles just that file. The folder it sits in is never mounted, so its sibling files stay out of the container. Use a dedicated folder for models with file attachments, because single-file `cratis run` mounts only the `.play` file.
- The file or folder is passed to Docker as a single argument, without a shell, so spaces and commas in the path are fine. A colon is not — Docker's `-v` option uses it as a separator — so a path containing one is rejected before Docker is started. A Windows drive letter is fine.
- The image tag is the Stage version this CLI renders with unless `--tag` asks for another. The Stage image compiles with its bundled Screenplay version, which can differ from the CLI renderer's Screenplay version. Running a single file needs a `cratis/stage` image of 3.16.0 or later.
- The Stage API is published on `http://localhost:9090` (change the host side with `--port`). Its API reference is at `http://localhost:9090/scalar/v1`.
- The **Chronicle Workbench** is published on `https://localhost:35000` (change the host side with `--workbench-port`), so you can inspect the session's events, observers and read models while it runs.
- The container is named `cratis-stage-<random>`, so a running sandbox is recognizable in `docker ps` and several can run side by side on different ports.
- `--rm` removes the container when it exits, so every run starts from a clean, in-memory store.

The Workbench is **HTTPS only** — open `https://localhost:35000`, not `http://`. The Chronicle port multiplexes
HTTP/1.1 and HTTP/2 through ALPN, which requires TLS, so a plain `http://` request to it returns nothing at all
(`ERR_EMPTY_RESPONSE` in a browser, `curl: (52) Empty reply from server`). The certificate is a self-signed
development one, so your browser warns the first time. Sign in with the Stage image's development credentials —
user `admin`, password `ChangeMeNow!`.

## Runtime engines

`semantic` is the default for `cratis run`, for both file and folder inputs. It evaluates validation, requirements, authorization, conditional production and constraints before accepting commands, and supports command routes. It refuses unsupported model capabilities rather than executing a partial model.

The Stage Host and Docker image also default to `semantic`. To keep the previous engine behavior, select the deprecated `eventmodel` fallback with `--engine eventmodel`. It logs a warning at startup and will be removed in Stage v6. It does not enforce validation, requirements, authorization, conditional production or constraints, and refuses routed models with `STAGE-ESM-030`.

```bash
cratis run ./screenplays
cratis run ./screenplays/invoicing.play --engine eventmodel
```

The CLI always passes `Stage__Runtime__Engine` to the container, independent of the image's default. A `--tag` older than the bundled Stage may not support this setting; when Stage reports its engine, the startup output shows what actually runs, not just what you requested.

A refusal stops the container and exits with `5`. The CLI does not restart or switch engines. If you explicitly selected `--engine eventmodel` and an issue points to semantic, the refusal suggestion includes the exact retry command, preserving your path and non-default options, for example:

```bash
cratis run "./my models" --engine semantic --port 9191
```

Stage sends commands through Arc command proxies, preserving API base paths, microservice and identity headers; `/stage/routes` includes command JSON schemas keyed by full command name.

The semantic sandbox is still disposable: restarting the container loses its in-memory state, and an exit-42 warm handoff resets its in-memory kernel. Semantic read models live in Stage's in-process world, not in Chronicle projections; the Workbench still shows the event log. See Stage's [semantic execution configuration and limitations](https://github.com/Cratis/Stage/blob/main/Documentation/docker/index.md#opt-in-to-semantic-execution).

## Application shell

A `layout` can declare the shell's `navigation topbar` and `navigation sidebar` regions, a `module` can inherit, `extend` or `override` them, a screen `toolbar` can carry `button`s that name a State Change `slice`, and a profile can select `sample data`. The live Stage draws them with the same Scene components the Studio preview and a rendered application do: `cratis run` renders them as authored.

```bash
cratis run ./Integration/Cli/Shell/model
```

- The topbar shows the application's items and logo. Going to another module swaps the sidebar for the module's effective navigation: the application's items when the module declares nothing, only its own when it `overrides`, the application's then its own when it `extends`. The module and the feature that hold the page are marked active, and an item that targets a module opens the module's first feature.
- A toolbar `button` that names a State Change slice opens the slice as a task dialog built from the command's schema (title, `State Change · <Slice>`, one labelled field per command property, `Cancel` and the button title). Submitting posts the command to the route the Stage registered; cancelling posts nothing.
- The address is the state: `#/OrderScreen` reloads or shares to the same module, sidebar and page, and the browser's back button returns to the previous feature.
- **The root template, its options and the appearance decide the shell.** A profile's `root template` (an own layout or template, or `root template blueprint Cratis.Blueprint.Default AppShell version "^1.0"`) is the shell the live Stage starts in, chosen by Scene's `resolveEffectiveShell` exactly as the Studio preview chooses it; `option showFooter false` leaves the footer out, `option navigationMode slim` starts the sidebar slim, and `appearance` (`color scheme light|dark|system`, `accent "#e11d48"`) sets the colour scheme and the theme's primary colour. A root template that cannot start is reported before Docker starts - the compiler's `PLAY0790`-`PLAY0799`, or `CLI-RENDER-019` for a version pin no installed blueprint satisfies - and the Stage never draws a different shell instead.
- **Sample data never reaches a running Stage.** A `sample data` pack and the profile's `active` selection exist to preview with at design time; the scene a running Stage serves, and the application `cratis render` publishes, carry no pack, no blueprint pack and no selection (spec SD6). `cratis run` has no option to switch it on.
- Before Docker starts, the compiler's errors about the shell are reported with their code and `file(line,column)` location, exactly as `cratis render` and `cratis screenplay validate` report them, and nothing is started (exit code 5): a navigation item whose target does not exist or is outside its module (`PLAY0748`, `PLAY0750`), a slot no layout declares (`PLAY0749`), a duplicate item id (`PLAY0747`), a toolbar button that names a slice that is not a State Change slice or does not exist (`PLAY0762`), and a sample data selection no pack satisfies (`PLAY0714`). See Screenplay's [navigation](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/navigation.md) and [sample data](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/sample-data.md) references for the full set.

The CLI's own CI (`scripts/test-shell-application.sh`) runs one fixture application with three modules (one inheriting, one overriding, one extending the sidebar) through `cratis run` on a live Stage (once with an own root template and once with the default blueprint's) and through `cratis render`, a build and a launch on the generated backend, and drives all of them with the same browser scenario, which also checks the shell options and the appearance; it then checks that `cratis run` and `cratis render` both refuse a root template whose version pin no installed blueprint satisfies.

## Custom packages

A `ui profile` can name packages beyond the five the Stage ships (`core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default`) once the application approves them in an [approved package set](screenplay.md#custom-runtime-packages) - the same document `cratis render --package-set` takes. `cratis run` hands it to the Stage, which loads the packages at runtime instead of compiling them in:

```bash
cratis run ./screenplays --package-set ./approved-packages.json --packages ./node_modules
```

- Before Docker starts, the `ui profile` is resolved against the shipped and the approved packages. A package that is neither is refused with `CLI-RENDER-007` (exit code 5) and nothing is started, so a typo or a forgotten approval never produces a sandbox that shows a placeholder where the package's component belongs.
- The set is mounted read-only at `/packages/approved-packages.json` and the folder of each approved package, `<packages folder>/<npm name>` (for example `./node_modules/@acme/scene.charts`), read-only at `/packages/<npm name>`. Only those folders are mounted, never the folder that holds them. A package whose folder is missing is refused before Docker starts.
- Each package folder holds the **browser build** of the package: an ES module (named by its `package.json` `exports`, `module` or `main`, else `index.js`) that exports the bundle the set's `module.export` names and leaves `react`, `react/jsx-runtime`, `react-dom` and `react-dom/client` external - the Stage resolves them to its own React so the package's hooks and contexts are the Stage's - plus the style sheets the set lists. Anything else the module imports must be bundled into it.
- The Stage serves only those folders, loads each module from the URL it reports itself, and checks the loaded bundle against the declaration in the set. A package that is missing, does not load, lacks its export or differs from its declaration stops the Stage with the reasons on the page; it never renders the profile with the package silently absent.
- Running an older Stage image (`--tag`) that predates package loading ignores the mounts and renders only the shipped packages. Stage 4.80.0 or later loads custom packages (the default tag of this CLI).

## Output

The container's output is hidden while it starts, and replaced by a progress
line. After the Stage API answers, the command polls `/stage/status` until
Stage reports `ready` or `unsupported`. The `warm` and `loading` states keep
waiting; merely listening on HTTP does not mean the model can run. A semantic
`ready` response confirms that registration and admission have completed.
For EventModel, or a response without an engine field, the command also waits
for read-model registration output or the existing 15-second grace period,
because EventModel can report `ready` while registration is still running.

When a ready session has ignored notes, table output adds one muted line, for
example `2 notes the engine ignored (use --verbose to list them)`. With
`--verbose`, or in plain output, the notes are listed under **Ignored by this
engine** with the same grouping as a refusal. Quiet output shows nothing.

A refusal stops the container started by the command and exits with validation
error code `5`, without restarting on another engine.
Table and plain output group similar issues by kind, replacing quoted names with
placeholders. The summary lists these categories in order:

- **Fix your model** — missing required example properties and timeline ordering problems.
- **Not supported by this engine yet** — other engine or language capability gaps.
- **Ignored by this engine** — authoring, realization or operational metadata, and explicitly deferred surfaces reported by Stage.

Each category shows its total issue count. Each kind shows its count and up to
three source locations (or artifact names), followed by the number of remaining
locations. All reported categories still block admission; the metadata label does
not turn a refusal into success. `--verbose` adds every original issue's
capability, artifact and details after the grouped summary.

A reducer body or a code-bodied validation rule does not refuse the whole session on the semantic
engine (Stage 4.145.0 and later): the Host refuses only the commands and queries that reach such a body,
with `STAGE-IMPL-003` naming it, and the rest of the model runs.

Newer Stage images (4.113.0 and later) also report `notes` from `/stage/status`:
compiler information and warning diagnostics that the engine ignored. Notes
never block admission. On a refusal, they join **Ignored by this engine**,
grouped like issues by their message with quoted names replaced, their
diagnostic code kept, and their source as the location. Older images that send
this information as issues keep the heading-based categories above.

JSON and compact JSON refusals are one document on stderr, with `error`, `message`,
`suggestion`, `engine`, the complete `issues` list, and `groups` containing
`category`, `kind`, `count` and `locations`. They retain every issue regardless of
`--verbose`. They also include the raw `notes` as Stage reported them, including
`code`, `message`, `source` and `severity`, which can be a string, a number or a
value from a newer image. The `groups` list combines issues and notes.

For older images that return HTTP 404 or an unparsable status body, readiness
falls back to the previous behavior: wait for read-model registration output,
or for its 15-second grace period after the API starts answering. The runtime
engine is shown only when Stage reports it.

If the container stops before it gets there, the error it reported is shown
along with the last lines it wrote, and the command exits with a server error.
Run with `--verbose` to stream the container's output as it happens instead —
useful when the captured tail is not enough to tell what went wrong.

With `-o json` or `-o json-compact`, nothing is printed until the session is
ready, and then a single object is emitted with the resolved endpoints. It
also includes an `engine` field when Stage reports one (for example,
`"engine": "semantic"`) and a `notes` array, empty when the image reports none:

```json
{
  "status": "ready",
  "engine": "semantic",
  "notes": [],
  "path": "/work/invoicing",
  "eventModel": "Invoicing",
  "eventStore": "gentle-zephyr",
  "stageApi": "http://localhost:9090",
  "apiReference": "http://localhost:9090/scalar/v1",
  "workbench": "https://localhost:35000"
}
```

## Stopping

The command keeps running until you stop it with `Ctrl+C`. It then stops the
container, waits for Docker to remove it, and exits with `0` — so the prompt
comes back only once the sandbox is actually gone. A second `Ctrl+C` terminates
the command immediately, leaving whatever Docker is doing to finish on its own.
When the session ends by itself, the command exits with a server error if the
container failed.

## Errors

| Condition | Result |
|---|---|
| `--engine` is not `eventmodel` or `semantic` | Settings validation error listing both values — nothing is started. |
| Stage reports `unsupported` | Validation error (exit code `5`) with all reported issues — the container is stopped. |
| The folder contains no `.play` files | Validation error — nothing is started. |
| The file or folder does not exist, or the path is invalid | Validation error — nothing is started. |
| The file does not have a `.play` extension | Validation error — nothing is started. The folder it sits in is not used instead. |
| The path contains a colon, other than a Windows drive letter | Validation error — nothing is started. |
| `docker` is not installed or not on `PATH` | Connection error with a hint to install Docker. |
