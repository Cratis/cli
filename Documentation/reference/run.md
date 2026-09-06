# Run

`cratis run` accepts a Screenplay (`.play`) file or a folder of Screenplay files for a local [Stage](https://github.com/Cratis/Stage) sandbox. It packages a Chronicle kernel, the Stage engine, and an in-memory event store into a single throwaway container so you can play with an event model straight from its `.play` source — no server to set up, nothing to clean up afterward.

```bash
cratis run [PATH]
```

```text
  Running Screenplay from /work/invoicing

  Stage API           http://localhost:9090
  API reference       http://localhost:9090/scalar/v1
  Chronicle Workbench https://localhost:35000
                      HTTPS only — sign in with admin / ChangeMeNow!

  ✓ Ready — event model 'Invoicing' as event store 'gentle-zephyr'
  Press Ctrl+C to stop
```

Point it at one `.play` file, a folder of `.play` files, or omit the path to use the current folder. The selected file or folder is mounted read-only. File input does not mount its parent folder, include sibling files, or follow implementation-file references.

```bash
cratis run ./screenplays
cratis run "./screenplays/invoicing model.play"
```

Single-file input requires a Stage image whose entrypoint accepts the supplied input path. This CLI input change alone does **not** establish support in a live published image; verify the actual image before relying on file input. Folder input searches recursively for `*.play` with case-insensitive matching on every platform.

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
| `PATH` | Screenplay (`.play`) file or folder to run. File extensions are checked case-insensitively. Folders are searched recursively using case-insensitive `*.play` matching on every platform. Defaults to the current directory. |

## Options

| Option | Description |
|---|---|
| `--tag <TAG>` | The `cratis/stage` image tag to run. Default: `latest`. |
| `--port <PORT>` | Host port to publish the Stage API on. Default: `9090`. |
| `--workbench-port <PORT>` | Host port to publish the Chronicle Workbench on. Default: `35000`. |
| `--verbose` | Stream the container's output instead of showing startup progress. |

Global options such as `-o/--output` are also accepted — see [Global Options](global-options.md).

## What it does

For a folder, the command mounts it read-only and publishes both of the container's ports to your host:

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel:ro cratis/stage:latest
```

For one file, the invocation instead mounts only that exact host file at a fixed path and passes the container path after the image:

```bash
docker run --rm --name cratis-stage-a1b2c3d4 -p 9090:9090 -p 35000:35000 -v "$PWD/invoicing.play":/eventmodel/input.play:ro cratis/stage:latest /eventmodel/input.play
```

Paths with spaces remain one process argument; the CLI does not use a shell. Commas in host names remain literal under the existing `-v` syntax. Colons in file or folder names are rejected before Docker rather than escaped or reinterpreted; a native Windows drive prefix is preserved. No new mount parser is introduced.

- A folder is mounted read-only at `/eventmodel` inside the container; Stage globs `**/*.play` beneath it and compiles them.
- A file is mounted read-only at `/eventmodel/input.play`; the host parent folder is never mounted, even if an older image ignores the input argument. This is not a claim of compatibility with such images.
- The Stage API is published on `http://localhost:9090` (change the host side with `--port`). Its API reference is at `http://localhost:9090/scalar/v1`.
- The **Chronicle Workbench** is published on `https://localhost:35000` (change the host side with `--workbench-port`), so you can inspect the session's events, observers and read models while it runs.
- The container is named `cratis-stage-<random>`, so a running sandbox is recognizable in `docker ps` and several can run side by side on different ports.
- `--rm` removes the container when it exits, so every run starts from a clean, in-memory store.

This input fix does not address image version pinning or sandbox runtime gaps. The default image tag remains the mutable `latest`; read-only input mounts are not a general isolation guarantee. Existing ports, readiness, cancellation, and session behavior are unchanged.

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
| The file or folder does not exist, or the path is invalid | Validation error — nothing is started. |
| An existing file does not have a `.play` extension | Validation error — nothing is started. |
| A file or folder name contains a colon unsupported by `-v` | Validation error — nothing is started. Native Windows drive prefixes are allowed. |
| `docker` is not installed or not on `PATH` | Connection error with a hint to install Docker. |
