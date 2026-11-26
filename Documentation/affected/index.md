---
title: Build and test only what a change can reach
description: Use cratis affected to select and run .NET tests a change can reach, refresh coverage maps on merge, and replay past changes to measure selection.
---

<a id="cratis-affected"></a>

`cratis affected` decides what a pull request has to build and test. In a large .NET repository most
projects and most tests have nothing to do with a given change, so running all of them wastes tens of
minutes. The command reads the project graph and a coverage map per test project, selects the test projects and
the tests a change can reach, and writes a report that says why each was chosen.

Any doubt resolves to running more, never less. A missing, corrupted or outdated map, an unknown file, or an
exception while selecting runs the whole project, and the command still exits successfully.

## Commands

```bash
cratis affected select    # decide what to run and write the report; runs nothing
cratis affected projects  # list the test projects, to split a merge-time run across jobs
cratis affected run       # select, then run; on the default branch, refresh the coverage maps
cratis affected replay    # replay merged changes to measure the selection
```

## Two levels of selection

| Level | Needs maps | What it does |
| --- | --- | --- |
| `project` | No | Test projects a change can reach run in full; the rest are skipped. |
| `test` (default) | Yes | Within an affected project, only the tests that cover a changed file run. A project without a usable map runs in full. |

Start with `--level project`. It needs no setup and removes the work of every unreachable test project.

## Which projects a change reaches

The command reads each project file at the base and at the tested commit. A project is affected when a file in
its directory changed, when it links a changed file from outside its directory, or when it depends, directly or
through others, on an affected project. These cases are handled explicitly:

- **Added project:** it and its dependents are affected, and a new test project always runs in full.
- **Removed project:** resolved against the base graph, so what depended on it is affected.
- **Renamed or moved project:** treated as a removal plus an addition.
- **Source generator or analyzer:** a change selects every test of every dependent test project, because
  coverage attributes generated code to generated files, not to repository files.

### What forces a full run

Changes to `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`,
`NuGet.config`, `.editorconfig`, `.globalconfig`, `.cratis/affected.json`, and any file outside every project
directory force a full run. Name a path in `mappedPaths` when another job already handles it, for example
documentation.

Configure the repository in `.cratis/affected.json`. Every key is optional.

```json
{
  "runEverythingTriggers": ["Directory.Build.props", "Directory.Packages.props", "global.json"],
  "mappedPaths": ["Documentation/**", "**/*.md"],
  "testProjects": [],
  "nonTestProjects": ["**/Fixtures/**"],
  "generatorProjects": [],
  "addedFilePolicy": "project",
  "unmappedFilePolicy": "project",
  "attributionGroup": "unit",
  "maxAttributionGroups": 0,
  "buildArguments": [],
  "implicitReferences": { "Source/Storage.Specs/**": ["Source/Server/Server.csproj"] },
  "rollupThreshold": 0.5,
  "rollupMinUnits": 5,
  "sampleRate": 0,
  "granularity": "class",
  "unionPreviousMap": false,
  "maxFilterLength": 100000
}
```

A project is a test project when it references `Microsoft.NET.Test.Sdk` or xUnit v3, or sets `IsTestProject` to `true`.
An explicit `IsTestProject` of `false` wins over a test package. List a convention that lives in shared props in
`testProjects`, and fixtures or samples that reference a test package without being run in `nonTestProjects`.
`buildArguments` are added to every `dotnet build` of a test project, for example
`["-p:RunAnalyzers=false", "-p:EnforceCodeStyleInBuild=false"]` when another job already runs the analyzers.
A test project that targets several frameworks needs one chosen with `--framework`.
`implicitReferences` names a project a test project uses without referencing it, such as a spec that starts the
server from its build output. The project is built before the test project, and a change to it reaches that test project.

Setting `runEverythingTriggers` replaces the default list, so keep a repository's own contract (such as a
committed list of build-affecting paths) in it rather than narrowing it.

## Coverage maps

A map records which source files each test unit executed. It is written by the full run on the default branch,
one file per test project, to `.cratis/affected/maps/<project path>.json`, and committed with the code.

- A unit is a test class by default; set `granularity` to `test` for individual tests.
- The text is deterministic: sorted keys, no timestamps, one unit per line. An unchanged rebuild produces no diff.
- It records the schema version, the commit it was built from (`builtAt`), a file table, and for each unit the
  files it `covers` and the files that declare it (`declaredIn`).
- Coverage is attributed by running each unit alone, serially, in its own process under a coverage collector.
  `dotnet-coverage` is the default; pass `--collector coverlet` to use coverlet.
  `--projects a,b` builds the maps of those test projects only, so a workflow can run one job per project.
  `--parallelism <count>` runs that many coverage runs at once, which a large project needs: Cratis Chronicle's
  Core specs take hours one at a time. coverlet rewrites the assemblies it measures, so it always runs one.
- A process per test class does not scale to thousands of classes. Set `attributionGroup` to `namespace` and the
  classes of one test namespace, cut at its first `for_` segment, run in one process and share its coverage. A
  change then selects the classes of the namespaces it reaches, never fewer than the exact map would. Measured on
  Cratis Arc, 2,685 classes in 281 namespaces took about 18 minutes instead of hours.
  A project with hundreds of namespaces still pays a process start per namespace: Cratis Chronicle's Core specs have
  547 and took four hours. `maxAttributionGroups` bounds the runs per project; neighboring namespaces then share a run,
  so selection is wider, never narrower. A group that fails is run once more before it fails the project's map.

Maps are never written from pull requests.

## How tests are selected

For each affected test project, the changed files are `git diff <map.builtAt>..HEAD`, so a stale map selects
more tests, not fewer.

1. Run all when the project matches a run-everything trigger, has no map, has an unknown schema version, or
   `builtAt` is not an ancestor of `HEAD` (a force push or a shallow clone).
2. Select every unit whose `covers` or `declaredIn` contains a modified, deleted or renamed-from file.
3. A file the map does not know, which is every added file, follows `addedFilePolicy`: `project` selects every unit
   that covers any file of the same project, `directory` narrows it to the directory, `ignore` selects nothing.
   A modified or deleted file that no unit of this test project covered follows `unmappedFilePolicy` the same way.
   Under `project`, the safe default, a change to a library selects every test of a dependent project that covered
   any file of that library. `directory` selects only tests that covered a neighbor of the file, which skips a
   dependent project that never ran the library's code. A file nothing executed can still be read by reflection or
   type discovery, so a repository that picks `directory` or `ignore` relies on its full merge and nightly runs.
4. Build the affected test project, discover its tests, and select every new one. Removed tests are ignored.
5. When at least `rollupThreshold` of a namespace is selected and it holds at least `rollupMinUnits` units, the
   selection is replaced by one entry for the namespace. Reaching the project root means run all.
6. A project is skipped only when it is not affected, or nothing was selected and no test is new.

`sampleRate` runs a deterministic share of the unselected tests as a canary.

## How the selection reaches the test runner

xUnit v3 test projects are executables. The selection becomes repeated `-filter` arguments, which the runner
combines with OR:

```text
dotnet Specs.dll -filter /*/My.Namespace/when_doing_something/* -filter /*/My.Other/when_other/*
```

Each query matches the namespace and class exactly, so a class never matches a sibling that shares its name as a
prefix. Long selections are split into batches of at most `maxFilterLength` characters, each run separately.

Before anything runs, the command asks the runner which tests the generated filter matches and compares that
with the computed selection. A difference runs the whole project and says why.

## Reports

`select` and `run` write `selection.json` and `selection.md` to `--report-directory` and append the Markdown to
the GitHub step summary when `GITHUB_STEP_SUMMARY` is set. The report names the mode, the selected and total counts,
and the first reasons for each test project.

## Replay a repository's history

```bash
cratis affected replay --branch origin/main --count 100 --failures failures.json
```

Each change is replayed against the maps committed before it. The report shows the median fraction of tests the
selection would have run and, when `failures.json` maps a commit to the tests that failed in its real run, every
failure the selection would have missed. The exit code is non-zero when a failure would have been missed.

## Exit codes

`select` always succeeds unless the repository or arguments are invalid. `run` returns the test result.
`replay` returns `DriftDetected` (7) when a replayed change would have missed a failing test.
