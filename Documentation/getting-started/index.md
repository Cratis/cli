# Getting Started

This page walks you through installing the `cratis` CLI, verifying it works, setting up your first connection context, and running your first commands against a Chronicle server.

## Installation

Install the CLI as a .NET global tool:

```bash
dotnet tool install -g Cratis.Cli
```

To upgrade an existing installation:

```bash
dotnet tool update -g Cratis.Cli
```

## Verification

Confirm the tool is on your PATH and check the installed version:

```bash
cratis --version
```

The command prints the CLI version. If it also successfully reaches a configured server, it prints the server version alongside it.

## Shell Completions

The CLI provides tab completions for all commands, options, and arguments. Install completions for your shell:

```bash
cratis completions install
```

By default, the command detects your current shell. Use `--shell` to target a specific shell:

```bash
cratis completions install --shell bash
cratis completions install --shell zsh
cratis completions install --shell fish
```

Use `--force` to overwrite an existing completion script:

```bash
cratis completions install --shell zsh --force
```

After installing, restart your shell or source your profile to activate completions.

## The get-started Command

Run `cratis get-started` at any time to check your configuration, test the server connection, and see a summary of useful commands:

```bash
cratis get-started
```

When no context is configured and no `CHRONICLE_CONNECTION_STRING` environment variable is set, the command shows a panel explaining the next steps — for example:

```
No context configured.
Run 'cratis context create <NAME> --server <CONNECTION_STRING>' to create one,
or set the CHRONICLE_CONNECTION_STRING environment variable.
```

When a context is configured and the server is reachable, the command confirms the connection and lists key commands for common tasks such as listing event stores, replaying observers, and inspecting failed partitions.

## Setting Up a Context

A context is a named, saved connection to a Chronicle server. Create one for your local development environment:

```bash
cratis context create dev --server chronicle://localhost:35000/?disableTls=true
```

Set it as the active context:

```bash
cratis context set dev
```

Verify the connection:

```bash
cratis get-started
```

The command confirms that the CLI can reach the server and shows the Chronicle server version.

For full context management — listing, renaming, deleting, and setting individual values — see the [Context](../context/index.md) page.

## Using an Environment Variable

As an alternative to contexts, set `CHRONICLE_CONNECTION_STRING` in your shell or CI environment:

```bash
export CHRONICLE_CONNECTION_STRING=chronicle://localhost:35000/?disableTls=true
```

The CLI reads this variable when no `--server` flag is provided and no active context is set. This is convenient for automation and containerized environments where managing a config file is not practical.

The full resolution order is: `--server` flag > `CHRONICLE_CONNECTION_STRING` > active context > default `chronicle://localhost:35000/?disableTls=true`.

## AI Tool Integration

Run `cratis init` to generate an LLM context file in the current directory. This file gives AI coding tools such as Claude Code, Cursor, or Windsurf a compact description of the CLI's capabilities and output formats, enabling them to write accurate `cratis` commands without guessing:

```bash
cratis init
```

The command writes the context file and prints the path. Add it to your project's AI configuration as instructed by your tool.
