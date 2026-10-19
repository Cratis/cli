---
title: Release notes
description: Read what changed in each release of the Cratis CLI, from the terminal, without going online.
---

`cratis release-notes` shows what changed in each release of the CLI. Every build carries the notes of every release up to its own, so it works offline and always matches the version you run.

```bash
cratis release-notes [--version <VERSION>] [--since <VERSION>]
```

## Read the history

In a terminal, `cratis release-notes` opens the latest release and lets you page through the history one release at a time:

| Key | Moves to |
| --- | --- |
| `→`, `n`, space, page down | the next older release |
| `←`, `p`, page up | the next newer release |
| home, end | the newest, the oldest release |
| `q`, escape | quit |

When the output is redirected or nobody can press keys, it prints the latest release instead, as markdown with `-o plain` or formatted otherwise.

## Options

| Option | Shows |
| --- | --- |
| `--version <VERSION>` | the notes of that release only; a release this build does not know exits with code 1 |
| `--since <VERSION>` | every release after that version, up to the one you run, newest first |
| `-o json` | every selected release as `{ version, notes, url }`, newest first |
| `-q` | the version of every selected release, one per line |

## After an update

`cratis update` shows what changed once it has installed a newer version. The version just installed is the one that knows its own notes, so `cratis update` runs it as `cratis release-notes --since <previous version>`:

- One release in between: its notes are printed.
- Several: you are asked whether to see them, then page through them. `--yes` skips the question.
- When the new version does not have its own notes yet, or cannot be started, you get a link to its page on [GitHub releases](https://github.com/Cratis/cli/releases), which always has them.

`-o json` and `-q` on `cratis update` show no notes.
