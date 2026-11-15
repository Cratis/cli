---
title: Cratis CLI command and options reference
description: Find Cratis CLI command syntax for inspecting event models, connecting AI tools, choosing output formats, and resolving Chronicle connections.
---

<a id="reference"></a>

The Cratis CLI reference covers application commands, AI integrations, and shared settings. Choose a task below to find command syntax and options, select an output format, or resolve a Chronicle connection.

<a id="topics"></a>

## Work with an application's event model

- [Run a Screenplay model](run.md): Start a `.play` model in a local Stage sandbox through Docker.
- [Stop a Stage sandbox](stop.md): Stop a sandbox that `cratis run --detach` left running.
- [View an application's event model](view.md): Open a .NET project's embedded or source-generated Screenplay documents in a browser without starting the application.
- [Generate, validate, and render Screenplay](screenplay.md): Generate `.play` documents from source, validate models, compare them with source or a Chronicle namespace, and render application code.
- [Capture and interpret an existing system with Prologue](prologue.md): Configure capture of selected system signals, then interpret the captures into a Screenplay model to review and continue authoring.

## Connect AI tools

Use the Model Context Protocol (MCP) references to connect an AI client to tools. Language-model settings serve a different purpose: they select the provider that Cratis tools such as Prologue use.

- [Configure a language-model provider](llm.md): Set, inspect, or clear the provider settings used by Cratis tools.
- [Run and register Screenplay MCP](screenplay-mcp.md): Start the local Screenplay authoring server and register it in an AI client.
- [Install Screenplay for desktop AI clients](screenplay-desktop-mcp.md): Install, update, inspect, or remove the local integration for Claude Desktop and ChatGPT Desktop, including the steps to complete in each host.
- [Manage extension packages](extension.md): Create, verify, restore, update and vendor the extension packages a Screenplay model references, with the lock, the allow-list and registry credentials.
- [Connect an AI client to Direct MCP](direct-mcp.md): Run the bridge to Direct's MCP endpoint using a stored Direct login, and manage the client registration.
- [Read the machine-readable command catalog](llm-context.md): Give an agent the command syntax, each command's effect, and whether it requires confirmation.

## Control output, connections, and versions

Shared options have command-specific behavior. For example, `cratis llm-context` always prints JSON, while MCP server invocations reserve standard output for protocol messages. Check the command's own reference before using shared flags in a script.

- [Choose shared command options](global-options.md): Look up `--output`, `--quiet`, `--yes`, `--debug`, and environment selection.
- [Choose an output format](output-formats.md): Select `table`, `plain`, `json`, or `json-compact` for terminal reading or automation.
- [Resolve a Chronicle connection](connection.md): Look up the connection string format and precedence of the server flag, environment variable, active context, and default.
- [Read CLI release notes](release-notes.md): Browse the bundled release history or select notes for a version or a range after an update.
