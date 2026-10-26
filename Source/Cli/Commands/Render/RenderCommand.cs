// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Plans and safely publishes one logical Screenplay application.
/// </summary>
[LlmDescription("Compiles a Screenplay file, folder, or canonical workspace into ESM, plans a complete bundled target before writing, and safely publishes only managed artifacts through a durable recovery journal, or checks publication read-only with --check. Reports release-baseline warnings without changing artifacts, receipts or exit codes.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("render", "Plan and safely publish a Screenplay application")]
[CliExample("render", "./plays", "--target", "cratis", "--destination", "./out", "--name", "MyApplication")]
[CliExample("render", "--workspace", "./application.workspace.json", "--destination", "./out")]
[CliExample("render", "./Model", "--name", "App", "--destination", "./src/App", "--check")]
[CliExample("render", "./Model", "--name", "App", "--destination", "./src/App", "--file-header", "Copyright (c) Acme {year}. All rights reserved.")]
[LlmOption("[PATH]", "string", "Screenplay (.play) file, or folder representing one logical application. If the model root has .screenplay/identities.json, renders its complete persisted workspace with Studio/MCP identities; a typed root-open failure reports CLI-RENDER-017, never temporary-identity fallback. Defaults to the current directory only without --workspace; mutually exclusive with --workspace.")]
[LlmOption("--workspace", "string", "Canonical Screenplay workspace envelope file, at most 32 MiB of actual input bytes. Preserves authoritative application name, identity catalog and documents; no archive extraction.")]
[LlmOption("--baseline", "string", "Release-baseline file for --workspace only. Overrides .screenplay/release-baseline.json beside the workspace file; an explicitly missing file warns. PATH input uses its model root's persisted metadata. Warnings do not change exit codes.")]
[LlmOption("--target", "string", "Statically bundled renderer target (default: cratis).")]
[LlmOption("--scope", "string[]", "Repeatable authored module, feature or slice address: Module.Feature.Slice. Unselected managed output is preserved; release-baseline warnings cover the whole application.")]
[LlmOption("--migrate-reducer", "string[]", "Repeatable read model to migrate from its classic IReducerFor<T> reducer to a self-reducing read model keeping the same observer id: Module.Feature.Slice.ReadModel or its semantic id. Needs an application-scope render of an existing destination and is refused with --scope (CLI-RENDER-026); deletes the old reducer file atomically with the writes and persists version 2 .cratis/stage/rendering.json. With --check, reports the migration as pending (exit 6). One way; already migrated read models are skipped.")]
[LlmOption("--destination", "string", "Managed artifact destination (default: ./out).")]
[LlmOption("--name", "string", "Required destination-independent application identity for plain source without persisted identities. For a persisted root, PATH still requires it and it must equal the saved application name (otherwise CLI-RENDER-018); the saved identities are authoritative. Optional with --workspace, where it must exactly match the supplied application name, never rename it.")]
[LlmOption("--project-name", "string", "Generated project and solution name (default: application name); does not change application identity.")]
[LlmOption("--root-namespace", "string", "Root namespace for generated C# (default: application name, independently of --project-name); does not change application identity.")]
[LlmOption("--file-header", "string[]", "Repeatable plain-text line, without comment markers, that every generated C#, TypeScript and JavaScript file starts with as a // comment; a value may hold several lines separated by \\n. {year} becomes the current UTC year. Default: no header. Configuration, markup and documentation files never get one. Mutually exclusive with --file-header-file.")]
[LlmOption("--file-header-file", "string", "Text file whose lines form the file header, as for --file-header. {year} becomes the current UTC year; an empty file means no header. Mutually exclusive with --file-header.")]
[LlmOption("--force", "bool", "Replace modified active managed files; never authorizes unmanaged overwrite or modified stale deletion.")]
[LlmOption("--check", "bool", "Plan and check against the real destination without writing or recovering: exit 0 when up to date, 6 (changes_pending) for writes, deletions or a manifest rewrite, 5 (validation_error) for planning failures, publication refusals or pending recovery. With --force, check whether a forced render would succeed.")]
[LlmOption("--strict", "bool", "Require strict acceptance after safe artifacts and reports are published. With --check, never writes and acceptance failure outranks drift. Requires bundled Stage acceptance support.")]
[LlmOutputAdvice("json-compact", "Reports deterministic plan/publication counts and typed diagnostics; unsafe plans commit no artifacts. Strict acceptance failure retains safe published artifacts and reports.")]
public class RenderCommand : AsyncCommand<RenderSettings>
{
    /// <summary>
    /// The destination used when none is given.
    /// </summary>
    public const string DefaultDestination = "out";

    /// <summary>
    /// The renderer target used when none is given.
    /// </summary>
    public const string DefaultRendererTarget = "cratis";

    readonly IScreenplayPlanning _planning;
    readonly IArtifactPublication _publication;
    readonly RenderAcceptance _acceptance;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCommand"/> class.
    /// </summary>
    public RenderCommand()
        : this(new ScreenplayPlanning(), new ArtifactPublisher())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCommand"/> class.
    /// </summary>
    /// <param name="planning">The complete semantic artifact planning.</param>
    /// <param name="publication">The managed artifact publication.</param>
    /// <param name="acceptance">The CLI acceptance seam, returning a failure reason independently of generation safety.</param>
    internal RenderCommand(IScreenplayPlanning planning, IArtifactPublication publication, Func<ScreenplayRenderPlan, bool, string?>? acceptance = null)
    {
        _planning = planning;
        _publication = publication;
        _acceptance = new(acceptance);
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, RenderSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();
        cancellationToken.ThrowIfCancellationRequested();
        ScreenplayWorkspace? workspace = null;
        string? sourcePath = null;
        if (settings.Workspace is not null)
        {
            if (settings.Path is not null)
            {
                OutputFormatter.WriteError(format, "--workspace and PATH are mutually exclusive", "Choose one input mode", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            try
            {
                workspace = await RenderWorkspaceInput.Read(Path.GetFullPath(settings.Workspace, currentDirectory), cancellationToken);
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                OutputFormatter.WriteError(format, "Workspace input file was not found", "Provide an existing file with --workspace", ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }
            catch (Exception exception) when (exception is InvalidScreenplayWorkspace or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                OutputFormatter.WriteError(format, "Workspace input is unreadable, invalid, or exceeds the 32 MiB envelope limit", "Provide a canonical version 1 workspace file with a matching catalog and revision", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            if (settings.Name is not null && !string.Equals(settings.Name, workspace.ApplicationName, StringComparison.Ordinal))
            {
                OutputFormatter.WriteError(format, "--name must exactly match the workspace application name", "Omit --name to preserve the workspace name and identity", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }
        }
        else
        {
            if (settings.Baseline is not null)
            {
                OutputFormatter.WriteError(format, "--baseline requires --workspace", "PATH renders use the model root's .screenplay/release-baseline.json and persisted identities", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            var resolved = PlayFileTargetResolver.Resolve(settings.Path, currentDirectory);
            if (!resolved.IsResolved)
            {
                OutputFormatter.WriteError(format, resolved.Error!, resolved.Suggestion, ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }

            sourcePath = resolved.Path;
            if (!IsValidApplicationName(settings.Name))
            {
                OutputFormatter.WriteError(
                    format,
                    "A valid --name is required and must be a C# identifier",
                    "Use a stable name such as --name MyApplication; the destination never defines application identity",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }
        }

        var destination = Path.GetFullPath(settings.Destination ?? DefaultDestination, currentDirectory);
        var target = settings.Target ?? DefaultRendererTarget;
        var approvedPackages = ApprovedPackageSet.Empty;
        if (settings.PackageSet is not null)
        {
            var read = await ApprovedPackageSetFile.Read(Path.GetFullPath(settings.PackageSet, currentDirectory), cancellationToken);
            if (read.Error is { } error)
            {
                OutputFormatter.WriteError(format, error, ApprovedPackageSetFile.Hint, read.NotFound ? ExitCodes.NotFoundCode : ExitCodes.ValidationErrorCode);
                return read.NotFound ? ExitCodes.NotFound : ExitCodes.ValidationError;
            }

            approvedPackages = read.Set!;
        }

        var fileHeader = await RenderFileHeader.Resolve(
            settings.FileHeader,
            settings.FileHeaderFile is null ? null : Path.GetFullPath(settings.FileHeaderFile, currentDirectory),
            DateTimeOffset.UtcNow.Year,
            cancellationToken);
        if (fileHeader.Error is { } headerError)
        {
            OutputFormatter.WriteError(format, headerError, RenderFileHeader.Hint, fileHeader.NotFound ? ExitCodes.NotFoundCode : ExitCodes.ValidationErrorCode);
            return fileHeader.NotFound ? ExitCodes.NotFound : ExitCodes.ValidationError;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var recovered = !settings.Check && await _publication.Recover(destination, cancellationToken);
            ScreenplayRenderPlan planned;
            if (workspace is null)
            {
                planned = await _planning.Plan(new(sourcePath!, settings.Name!, target, settings.ProjectName, settings.RootNamespace) { Destination = destination, Scope = settings.Scope, MigrateReducers = settings.MigrateReducer, ApprovedPackages = approvedPackages, FileHeader = fileHeader.Lines }, cancellationToken);
            }
            else
            {
                var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
                var request = new ScreenplayDocumentRenderRequest(
                    SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog),
                    workspace.ApplicationName,
                    target,
                    settings.ProjectName,
                    settings.RootNamespace)
                {
                    Destination = destination,
                    Scope = settings.Scope,
                    MigrateReducers = settings.MigrateReducer,
                    ApprovedPackages = approvedPackages,
                    FileHeader = fileHeader.Lines,
                    ReleaseBaseline = new FileReleaseBaseline(ReleaseBaselineFile.Read(
                        settings.Baseline ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(settings.Workspace!, currentDirectory))!, ".screenplay", "release-baseline.json"),
                        workspace.IdentityCatalog.Application,
                        required: settings.Baseline is not null))
                };
                planned = _planning.PlanDocuments(request, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (planned.Documents == 0)
            {
                OutputFormatter.WriteError(
                    format,
                    $"No Screenplay ({PlayFileTargetResolver.Extension}) files found in '{sourcePath}'",
                    $"Point the command at a {PlayFileTargetResolver.Extension} file, or at a folder holding one",
                    ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }

            ScreenplayDiagnosticsWriter.Write(format, planned.Diagnostics);
            if (!planned.Success)
            {
                var errors = planned.Diagnostics.Count(_ => _.Severity == ScreenplayDiagnosticSeverity.Error);
                OutputFormatter.WriteError(
                    format,
                    $"{(settings.Check ? "Nothing was checked or written" : "Nothing was published")} — planning reported {errors} error(s)",
                    "Fix the reported compiler, capability, target, or artifact errors and render again",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (settings.Check)
            {
                var checkedPublication = await _publication.Check(new(planned.Artifacts!, destination, settings.Force) { SemanticAddresses = planned.SemanticAddresses, Selection = planned.Selection, ScaffoldOptions = planned.ScaffoldOptions }, cancellationToken);
                ScreenplayDiagnosticsWriter.Write(format, [.. checkedPublication.Receipt.Changes.Where(change => change.Code is not null)
                    .Select(change => new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, change.Code!, change.Reason!, change.Path))]);
                WriteCheckResult(format, target, destination, planned, checkedPublication);
                if (checkedPublication.Refused > 0)
                {
                    return ExitCodes.ValidationError;
                }

                // A real render rewrites the ownership manifest whenever its bytes differ, even with no artifact to write.
                var manifestChanges = checkedPublication.Receipt.Manifest is { } manifest &&
                    !string.Equals(manifest.BaseSha256, manifest.Sha256, StringComparison.Ordinal);
                return _acceptance.Complete(format, settings, planned, checkedPublication.Written + checkedPublication.Removed > 0 || manifestChanges ? ExitCodes.ChangesPending : ExitCodes.Success);
            }

            var published = await _publication.Publish(new(planned.Artifacts!, destination, settings.Force) { SemanticAddresses = planned.SemanticAddresses, Selection = planned.Selection, ScaffoldOptions = planned.ScaffoldOptions }, cancellationToken);
            WriteResult(format, target, destination, planned, published, recovered || published.Recovered);
            return _acceptance.Complete(format, settings, planned);
        }
        catch (UnsafeArtifactPublication exception)
        {
            if (exception.DiagnosticCode is { } code)
            {
                ScreenplayDiagnosticsWriter.Write(format, [new(ScreenplayDiagnosticSeverity.Error, code, exception.Message, null)]);
            }
            OutputFormatter.WriteError(
                format,
                exception.Message,
                "Resolve the ownership, modification, schema, or recovery conflict and render again",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    static bool IsValidApplicationName(string? name) => !string.IsNullOrWhiteSpace(name) &&
        (char.IsAsciiLetter(name[0]) || name[0] == '_') && name.All(_ => char.IsAsciiLetterOrDigit(_) || _ == '_');

    static void WriteCheckResult(
        string format,
        string target,
        string destination,
        ScreenplayRenderPlan planned,
        ArtifactPublicationCheckResult checkedPublication)
    {
        OutputFormatter.WriteObject(
            format,
            new
            {
                Target = target,
                Destination = destination,
                planned.Artifacts!.ApplicationName,
                planned.Documents,
                Artifacts = checkedPublication.Artifacts ?? planned.Artifacts.Artifacts.Length,
                checkedPublication.Written,
                checkedPublication.Removed,
                checkedPublication.Unchanged,
                checkedPublication.Refused,
                checkedPublication.RecoveryPending,
                Publication = checkedPublication.Receipt
            },
            result =>
            {
                AnsiConsole.MarkupLine($"[bold]{result.Destination.EscapeMarkup()}[/]");
                AnsiConsole.WriteLine($"Target: {result.Target}; documents: {result.Documents}; artifacts: {result.Artifacts}");
                AnsiConsole.WriteLine($"Check: {result.Written} write(s), {result.Removed} deletion(s), {result.Unchanged} unchanged, {result.Refused} refusal(s); recovery pending: {result.RecoveryPending}");
                foreach (var verdict in result.Publication.Changes)
                {
                    AnsiConsole.WriteLine($"{verdict.Kind}: {verdict.Path ?? "(destination)"}{(verdict.Reason is null ? string.Empty : $" — {verdict.Reason}")}");
                }
            });
    }

    static void WriteResult(
        string format,
        string target,
        string destination,
        ScreenplayRenderPlan planned,
        ArtifactPublicationResult published,
        bool recovered)
    {
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            Console.WriteLine(destination);
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Target = target,
                Destination = destination,
                planned.Artifacts!.ApplicationName,
                planned.Documents,
                Artifacts = published.Written + published.Unchanged,
                published.Written,
                published.Removed,
                published.Unchanged,
                Recovered = recovered,
                Publication = published.Receipt
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Destination.EscapeMarkup()}[/]\n" +
                    $"Target:    {result.Target}\n" +
                    $"Documents: {result.Documents}\n" +
                    $"Artifacts: {result.Artifacts}\n" +
                    $"Written:   {result.Written}\n" +
                    $"Removed:   {result.Removed}\n" +
                    $"Unchanged: {result.Unchanged}\n" +
                    $"Recovered: {result.Recovered}");
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel(content)
                    .Header(" Rendered ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0));
            });
    }
}
