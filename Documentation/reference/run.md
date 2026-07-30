# Run

`cratis run` boots a local [Stage](https://github.com/Cratis/Stage) sandbox from the Screenplay (`.play`) files in a folder. It packages a Chronicle kernel, the Stage engine, and an in-memory event store into a single throwaway container so you can play with an event model straight from its `.play` source — no server to set up, nothing to clean up afterward.

```bash
cratis run [PATH]
```

```text
  Starting Stage from /work/invoicing on http://localhost:9090
  Chronicle Workbench on https://localhost:35000
```

You point it at a folder of `.play` files (or run it from inside one), and it hands that folder to the Stage container, which compiles every `.play` file it finds and exposes a live API you can drive — plus the Chronicle Workbench, for looking at what the model does to the event store.

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

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## What it does

The command mounts the folder into the Stage container and publishes both of the container's ports to your host:

```bash
docker run --rm -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel cratis/stage:latest
```

- The folder is mounted at `/eventmodel` inside the container; Stage globs `**/*.play` beneath it and compiles them.
- The Stage API is published on `http://localhost:9090` (change the host side with `--port`). Its API reference is at `http://localhost:9090/scalar/v1`.
- The **Chronicle Workbench** is published on `https://localhost:35000` (change the host side with `--workbench-port`), so you can inspect the session's events, observers and read models while it runs.
- `--rm` removes the container when it exits, so every run starts from a clean, in-memory store.

The Workbench is served over HTTPS with a self-signed development certificate, so your browser will warn about it
the first time. Sign in with the Stage image's development credentials — user `admin`, password `ChangeMeNow!`.

The command streams the container's output and exits with the container's exit code. Stop the session with `Ctrl+C`.

## Errors

| Condition | Result |
|---|---|
| The folder contains no `.play` files | Validation error — nothing is started. |
| The folder does not exist | Validation error. |
| `docker` is not installed or not on `PATH` | Connection error with a hint to install Docker. |
