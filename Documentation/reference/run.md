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
  Press Ctrl+C to stop
```

You point it at a folder of `.play` files (or run it from inside one), or at a single `.play` file, and it hands exactly that to the Stage container, read-only. The Stage compiles the file, or every `.play` file in the folder as one application, and exposes a live API you can drive — plus the Chronicle Workbench, for looking at what the model does to the event store.

```bash
cratis run ./screenplays
cratis run "./screenplays/invoicing model.play"
```

The container's own output is kept out of the way. While it boots you get a
progress line — pulling the image, starting Chronicle, compiling the Screenplay
files, registering read models — and `Ready` appears once the Stage API answers
and the model's read models are registered, so anything you send it from that
point on is actually served.

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
| `--tag <TAG>` | The `cratis/stage` image tag to run. Default: the Stage version this CLI renders with. |
| `--port <PORT>` | Host port to publish the Stage API on. Default: `9090`. |
| `--workbench-port <PORT>` | Host port to publish the Chronicle Workbench on. Default: `35000`. |
| `--package-set <FILE>` | Approved package set (JSON) naming the custom runtime packages the `ui profile` may use, beyond `core`, `scene.web` and `Cratis.Components`. The Stage loads them at runtime. See [Custom packages](#custom-packages). |
| `--packages <FOLDER>` | Folder holding a folder for each approved package, named by its npm name (for example `./node_modules`). Default: the folder the package set is in. Needs `--package-set`. |
| `--verbose` | Stream the container's output instead of showing startup progress. |

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## What it does

For a folder, the command mounts it read-only into the Stage container and publishes both of the container's ports to your host (replace `<STAGE_TAG>` with the Stage version bundled with your CLI):

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel:ro cratis/stage:<STAGE_TAG>
```

For a single file, it mounts only that file, read-only, at a fixed path and passes that path to the Stage after the image:

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD/invoicing.play":/eventmodel/input.play:ro cratis/stage:<STAGE_TAG> /eventmodel/input.play
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

## Custom packages

A `ui profile` can name packages beyond the three the Stage ships (`core`, `scene.web` and `Cratis.Components`) once the application approves them in an [approved package set](screenplay.md#custom-runtime-packages) - the same document `cratis render --package-set` takes. `cratis run` hands it to the Stage, which loads the packages at runtime instead of compiling them in:

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
line. `Ready` is printed once the Stage API answers *and* the model's read
models have been registered with Chronicle — the API starts listening a few
seconds before that, so waiting for the registration is what makes `Ready` mean
the session is usable.

If the container stops before it gets there, the error it reported is shown
along with the last lines it wrote, and the command exits with a server error.
Run with `--verbose` to stream the container's output as it happens instead —
useful when the captured tail is not enough to tell what went wrong.

With `-o json` or `-o json-compact`, nothing is printed until the session is
ready, and then a single object is emitted with the resolved endpoints:

```json
{
  "status": "ready",
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
| The folder contains no `.play` files | Validation error — nothing is started. |
| The file or folder does not exist, or the path is invalid | Validation error — nothing is started. |
| The file does not have a `.play` extension | Validation error — nothing is started. The folder it sits in is not used instead. |
| The path contains a colon, other than a Windows drive letter | Validation error — nothing is started. |
| `docker` is not installed or not on `PATH` | Connection error with a hint to install Docker. |
