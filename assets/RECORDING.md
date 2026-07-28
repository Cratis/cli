# Recording the README GIFs

Everything in this directory that ends in `.tape` is a script. Nothing was performed by hand,
nothing was screen-captured, and any of it re-renders from a clean checkout:

```bash
dotnet build -c Release
assets/demo-store/reset.sh
vhs assets/demo.tape          # or palette, completions
```

That is the point. A hand-recorded screencast is a one-off you cannot fix a typo in; a tape is
source you can edit and re-run.

## The tools

[**vhs**](https://github.com/charmbracelet/vhs) turns a declarative `.tape` file into a GIF. It
spawns a real terminal, sends real keystrokes and records what actually happened, so the
numbers in these GIFs are the numbers that server really had.

```bash
brew install vhs      # pulls in ffmpeg and ttyd
```

**ffmpeg** comes along and does double duty: pulling frames out for inspection, and re-timing a
finished GIF if one ever needs it. None of the three currently do — what is committed is
exactly what vhs produced, so a re-render is byte-comparable.

## The fixture

A CLI that reads a server is only as interesting as the server. An empty Chronicle has nothing
to show, and the only populated one on the machine these were recorded on belonged to a real
customer system — so [`demo-store/`](demo-store/) builds a third option: a throwaway Chronicle
in Docker with a small bookshop seeded into it.

```bash
assets/demo-store/reset.sh          # fresh server, seed, exit
assets/demo-store/reset.sh drip 40  # append a trickle against a running one
```

It registers eight books, three members, two projections, two reducers and one reactor, then
appends a story: borrowings, two returns, two reservations, two books going overdue. The
reactor that sends overdue notices **throws for exactly one book**, which is what leaves a
failed partition behind for the CLI to find.

Two properties matter:

- **Deterministic identifiers.** Event source ids are generated from a fixed pattern rather
  than `Guid.NewGuid()`, so `00000014-1111-4222-8333-444444444444` is the same book on every
  re-render and the tapes can name it directly.
- **The failure survives.** Chronicle retries a failed partition on its own with a widening
  backoff, and it *succeeds* the moment the client reconnects without the fault. So the seeder
  exits after seeding and the partition stays failed. An earlier version of the hero tape ran a
  background writer during the workbench segment to make the dashboard move on camera — it
  reconnected the client, Chronicle retried, and the failure the GIF was about quietly
  disappeared. Frames caught it.

The project sits outside the CLI solution on purpose and carries its own `Directory.Build.props`
so MSBuild does not walk up into the repository's analyzers and central package versions.

## The loop

The order matters more than any individual setting.

**1. Read the key handling before writing a line of tape.** For the workbench that meant
`WorkbenchKeyDispatcher.Dispatch`. It is also how two stale claims turned up before they could
be filmed: the workbench's own `[LlmDescription]` and its Overview panel both told you to press
number keys to switch views, and no number key was bound anywhere in the dispatcher. Both are
fixed — but only because writing the tape meant reading the dispatcher first.

**2. Find the environment's opt-out switches.** This CLI has an unusually strong one: it
detects agent environments through `CLAUDECODE`, `CURSOR_TRACE_DIR`, `WINDSURF_SESSION_ID` and
friends, and silently switches to compact JSON with no banner. Recording from inside an agent
session without unsetting those produces a GIF of machine output.

`CRATIS_NO_UPDATE_CHECK=1` silences the update hint, which would otherwise close every command
in every clip with `↑ update available`. That switch did not exist when these were first
recorded — the tapes pre-dated the cache the check consults instead, which worked but was a
hack aimed at the wrong layer. Needing it here was the argument for adding it.

[`prepare-env.sh`](prepare-env.sh) handles the rest, and builds an isolated `$HOME` because
`~/.cratis/config.json` on a real machine holds real server addresses and client secrets.

**3. Rehearse, and look at the frames. Actually look at them.**

```bash
ffmpeg -i out.gif -vf fps=1 frames/%03d.png
```

Every problem in this set was found by looking, never by reasoning about what the tape should
have done:

- A `\\` line continuation that vhs typed literally, so the command ran as three broken
  fragments.
- The workbench opening with an **empty content pane** on a cold start, because the call that
  paints the content was guarded behind a restored view index greater than zero. Fixed rather
  than worked around.
- The generated bash completion script failing on `_init_completion`, which comes from the
  bash-completion package that the macOS system bash 3.2 does not have.
- Two clips rendered against a server that had died between renders, showing `0 items` where
  the content should be.

To find *where* something changed without scrubbing by eye, hash the frames and print only the
ones that differ from their predecessor:

```bash
ffmpeg -v error -i out.gif -vf fps=1 seq/%03d.png
python3 -c "
import hashlib, glob
prev = None
for f in sorted(glob.glob('seq/*.png')):
    h = hashlib.md5(open(f,'rb').read()).hexdigest()
    if h != prev: print(f'{int(f[-7:-4])-1:3d}s  {f}')
    prev = h
"
```

**4. Verify the server afterwards.** Capture the tail sequence number, the failed-partition
count and every observer's position before and after a render, and diff them. For this set both
sides were identical, which is how "nothing was changed" became a checked fact rather than a
hope.

## Arrow keys do not work here

On the machine these were recorded on, **vhs cannot deliver arrow keys to the application at
all.** This is not a workbench problem. The decisive test was bash itself:

```tape
Type "echo FIRST" Enter
Type "echo SECOND" Enter
Up
Up
```

Two `Up` presses recalled zero history. Readline is the most reliable arrow consumer there is,
so the keys are being lost below the application.

Everything else arrives: printable characters, `Enter`, `Escape`, `Tab` and `Ctrl+<key>`. So
the workbench clips are choreographed entirely out of those — `Ctrl+P` to open the palette,
typing to drive it, `F` to filter, `Escape` and `q` to leave. `Tab` is avoided inside the
workbench specifically, because it moves focus somewhere that blanks the content pane.

Two consequences worth knowing before editing a tape:

- The keys table in the README documents arrows because they work for real users. They are not
  demonstrable here.
- The workbench opens on the view it was last left on, so `prepare-env.sh` writes a
  `workbench-state.json` and the clips start where they want to be rather than navigating on
  camera. `RECORDING_NAV_INDEX` overrides which view that is.

## The settings, and what each is for

All of them live in [`_style.tape`](_style.tape), which every other tape pulls in with
`Source assets/_style.tape`. Three recordings that drift apart in font size or theme read as
three screencasts; one shared file makes them read as one set.

| Setting | Why |
|---|---|
| `Set FontSize 14` + `1400x800` | 143 columns by 42 rows. Enough for the workbench's sidebar, table and detail pane side by side without the table truncating everything; GitHub scales a README image to about 880px, and this is still legible there. |
| `Set Theme {…}` | Hand-matched to `WorkbenchColors.cs` and `OutputFormatter.cs` so the terminal chrome and the application agree. |
| `Set WindowBar Colorful`, `BorderRadius 10`, `Margin 24`, `MarginFill` | The polish. Costs nothing, and a bare rectangle of terminal looks unfinished next to it. |
| `Set Framerate 24` | 50 is the default and roughly doubles the file for no visible gain. |
| `Set TypingSpeed` | 55ms globally, dropped to 18ms for a 36-character partition id nobody reads, raised to 160ms where the typing *is* the content. |

`_style.tape` deliberately does **not** set the shell and does **not** close its `Hide` block.
Each tape picks its own shell and writes its own prompt, because a bash prompt and a zsh prompt
are not written the same way — and `completions.tape` has to be zsh.

## vhs gotchas

- `Output /abs/path.gif` **fails to parse**. Quote it, or use a relative path.
- `Escape`, not `Esc`. `Ctrl+P`, not `C-p`.
- `Set` works mid-tape, so `Set TypingSpeed 18ms` can speed up one stretch.
- A `\\` inside `Type "…"` is typed literally — it is not a shell line continuation. Put long
  commands on one line.
- `vhs validate file.tape` catches the syntax in a second. Run it before every render.

## What earns a GIF

The most useful discipline here was editorial, not technical.

**One hero, then short single-purpose clips.** The top of the README gets the full arc — the
verdict, the exception behind it, the live view — at about 35 seconds. Everything after is
15-20 seconds and shows exactly one thing, sitting next to the prose that explains it.

**A GIF has to show something text cannot.** That is the whole test.

- ✅ `demo.gif` — `diagnose` printing a verdict and naming the next command, then a full-screen
  dashboard painting itself over the same terminal and narrowing live as a filter is typed.
- ✅ `palette.gif` — one word matching an observer, an event type, a projection, a read model
  and a failure *at the same time*. A screenshot shows six rows; it cannot show them arriving
  as the query resolves.
- ✅ `completions.gif` — a tab press that makes a network call. There is no way to convey that
  the list came from the server rather than from the script, except by watching it happen.
- ❌ **The failed-partition trail on its own.** Four attempts of the same stack trace is a wall
  of output. It is in the hero, where it lasts seven seconds and carries the narrative, and the
  README quotes it as text where it is searchable and loads instantly.
- ❌ **The output formats.** The same command four ways is a table with byte counts, not a
  recording.

**Watch the total weight.** Three GIFs, 1.1 MB.

## Recording something destructive

`completions.tape` types `cratis chronicle observers replay ` on camera — a command that
reprocesses an observer from sequence zero. It exists in the clip because an observer id is the
best demonstration of completion hitting the server, and it is safe because **the tape never
sends `Enter`**. It ends on `Ctrl+C`, which abandons the line.

Audit before rendering:

```bash
awk '/^Show/{s=1} s' assets/completions.tape | grep -n "Enter"
```

That should return nothing but comment lines. Then verify the server afterwards, as above.

No README asset justifies a tape typo replaying somebody's observer.
