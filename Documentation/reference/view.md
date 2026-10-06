---
title: View
description: Open a .NET application as an event model in the browser, from the Screenplay documents it embeds or ones generated from its source.
---

`cratis view` opens an application as an event model in your browser. Run it in the folder of a `.csproj`: it starts a local web server hosting the same read-only explorer an [Arc](https://github.com/Cratis/Arc) application serves at `/.cratis/event-model/`, and opens a browser window on it.

```bash
cratis view [PATH]
```

```text
  Project       /work/Library/Library.csproj
  Assembly      /work/Library/bin/Debug/net10.0/Library.dll
  Documents     5 embedded (Library)

  Event model viewer http://127.0.0.1:52817/
  Press Ctrl+C to stop.
```

Use it to look at the model behind an application you are working on - its modules, features, and the commands, events and read models of each slice - without running the application or keeping a diagram up to date.

## Where the documents come from

The viewer shows Screenplay documents: one for the assembly, one per module and one per feature.

- **Embedded.** When the project's built output assembly embeds Screenplay documents - it references `Cratis.Arc.Screenplay.Embedded`, directly or through the `Cratis` metapackage - those are what you see. Assemblies the output references and that sit beside it in the output folder are read too, so a host project shows the documents of the domain projects it references.
- **Generated from source.** When the output embeds no documents, or the project has not been built, the documents are generated from the project's source with the same Arc generator the build uses, held in memory, and never written anywhere. The project and the projects it references need to be restored.

Pass `--from-source` to generate from source even when the output embeds documents, for example to see changes you have not built yet.

Generating reports the same diagnostics as [Screenplay generation](screenplay.md); the first ten errors and warnings are listed before the viewer starts.

## Arguments

| Argument | Description |
|---|---|
| `PATH` | The project file (`.csproj`), or the folder holding exactly one. The folder is not searched beneath. Defaults to the current directory. |

## Options

| Option | Description |
|---|---|
| `-c, --configuration <CONFIGURATION>` | The build configuration whose output assembly is read. Default: `Debug`. |
| `-f, --framework <FRAMEWORK>` | The target framework to read, for a project that targets several. Default: the first one the project lists. |
| `--port <PORT>` | The local port to serve the viewer on. Default: `0`, which picks a free port. |
| `--from-source` | Generate the documents from source even when the output assembly embeds some. |
| `--no-browser` | Serve the viewer without opening a browser window. |

Global options such as `-o/--output` are also accepted - see [Global Options](global-options.md). With a JSON output format, the command writes the viewer's address, where the documents came from and how many there are, then keeps serving.

## How it runs

- The output path is read by evaluating the project with `dotnet msbuild`, so custom output paths and artifacts layouts are honored. Nothing is restored or built.
- The output assembly is read for its embedded resources only - none of its code runs, and the file is not locked, so you can rebuild while the viewer is open.
- The server listens on the loopback interface (`127.0.0.1`) only, and keeps running until you press Ctrl+C.

The viewer itself - navigation, view options, the source view and the endpoints it calls - is described in [Browse your embedded event model](/arc/backend/csharp/embedded-event-model/).
