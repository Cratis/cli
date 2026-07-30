# Run

`cratis run` boots a local [Stage](https://github.com/Cratis/Stage) sandbox from the Screenplay (`.play`) files in a folder. It packages a Chronicle kernel, the Stage engine, and an in-memory event store into a single throwaway container so you can play with an event model straight from its `.play` source — no server to set up, nothing to clean up afterward.

```bash
cratis run [PATH]
```

```text
  Running the Screenplay files in /work/invoicing

  Stage API           http://localhost:9090
  API reference       http://localhost:9090/scalar/v1
  Chronicle Workbench https://localhost:35000
                      HTTPS only — sign in with admin / ChangeMeNow!

  ✓ Ready — event model 'Invoicing' as event store 'gentle-zephyr'
  Press Ctrl+C to stop
```

You point it at a folder of `.play` files (or run it from inside one), and it hands that folder to the Stage container, which compiles every `.play` file it finds and exposes a live API you can drive — plus the Chronicle Workbench, for looking at what the model does to the event store.

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
| `PATH` | Folder containing the Screenplay (`.play`) files to run. Searched recursively. Defaults to the current directory. |

## Options

| Option | Description |
|---|---|
| `--tag <TAG>` | The `cratis/stage` image tag to run. Default: `latest`. |
| `--port <PORT>` | Host port to publish the Stage API on. Default: `9090`. |
| `--workbench-port <PORT>` | Host port to publish the Chronicle Workbench on. Default: `35000`. |
| `--verbose` | Stream the container's output instead of showing startup progress. |

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## What it does

The command mounts the folder into the Stage container and publishes both of the container's ports to your host:

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel cratis/stage:latest
```

- The folder is mounted at `/eventmodel` inside the container; Stage globs `**/*.play` beneath it and compiles them.
- The Stage API is published on `http://localhost:9090` (change the host side with `--port`). Its API reference is at `http://localhost:9090/scalar/v1`.
- The **Chronicle Workbench** is published on `https://localhost:35000` (change the host side with `--workbench-port`), so you can inspect the session's events, observers and read models while it runs.
- The container is named `cratis-stage-<random>`, so a running sandbox is recognizable in `docker ps` and several can run side by side on different ports.
- `--rm` removes the container when it exits, so every run starts from a clean, in-memory store.

The Workbench is **HTTPS only** — open `https://localhost:35000`, not `http://`. The Chronicle port multiplexes
HTTP/1.1 and HTTP/2 through ALPN, which requires TLS, so a plain `http://` request to it returns nothing at all
(`ERR_EMPTY_RESPONSE` in a browser, `curl: (52) Empty reply from server`). The certificate is a self-signed
development one, so your browser warns the first time. Sign in with the Stage image's development credentials —
user `admin`, password `ChangeMeNow!`.

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
| The folder does not exist | Validation error. |
| `docker` is not installed or not on `PATH` | Connection error with a hint to install Docker. |
