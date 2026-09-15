// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Plans and safely publishes one logical Screenplay application.
/// </summary>
[LlmDescription("Compiles a Screenplay file, folder, or canonical workspace into ESM, plans a complete bundled target before writing, and safely publishes only managed artifacts through a durable recovery journal.")]
[CliCommand("render", "Plan and safely publish a Screenplay application")]
[CliExample("render", "./plays", "--target", "cratis", "--destination", "./out", "--name", "MyApplication")]
[CliExample("render", "--workspace", "./application.workspace.json", "--destination", "./out")]
[LlmOption("[PATH]", "string", "Screenplay (.play) file, or folder representing one logical application. Defaults to the current directory only without --workspace; mutually exclusive with --workspace.")]
[LlmOption("--workspace", "string", "Canonical Screenplay workspace envelope file, at most 32 MiB of actual input bytes. Preserves authoritative application name, identity catalog and documents; no archive extraction.")]
[LlmOption("--target", "string", "Statically bundled renderer target (default: cratis).")]
[LlmOption("--destination", "string", "Managed artifact destination (default: ./out).")]
[LlmOption("--name", "string", "Required destination-independent application identity for plain source; optional with --workspace, where it must exactly match the supplied application name, never rename it.")]
[LlmOption("--project-name", "string", "Generated project and solution name (default: application name); does not change application identity.")]
[LlmOption("--root-namespace", "string", "Requested rendering root namespace (default: application name); Stage 3.11 does not apply overrides to all generated C# files.")]
[LlmOption("--force", "bool", "Replace modified active managed files; never authorizes unmanaged overwrite or modified stale deletion.")]
[LlmOutputAdvice("json-compact", "Reports deterministic plan/publication counts and typed diagnostics; failed plans commit no artifacts.")]
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
    internal RenderCommand(IScreenplayPlanning planning, IArtifactPublication publication)
    {
        _planning = planning;
        _publication = publication;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, RenderSettings settings, CancellationToken cancellationToken)
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
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var recovered = await _publication.Recover(destination, cancellationToken);
            ScreenplayRenderPlan planned;
            if (workspace is null)
            {
                planned = await _planning.Plan(new(sourcePath!, settings.Name!, target, settings.ProjectName, settings.RootNamespace), cancellationToken);
            }
            else
            {
                var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
                var request = new ScreenplayDocumentRenderRequest(
                    SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog),
                    workspace.ApplicationName,
                    target,
                    settings.ProjectName,
                    settings.RootNamespace);
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
                    $"Nothing was published — planning reported {errors} error(s)",
                    "Fix the reported compiler, capability, target, or artifact errors and render again",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var published = await _publication.Publish(new(planned.Artifacts!, destination, settings.Force), cancellationToken);
            WriteResult(format, target, destination, planned, published, recovered);
            return ExitCodes.Success;
        }
        catch (UnsafeArtifactPublication exception)
        {
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
                Artifacts = planned.Artifacts.Artifacts.Length,
                published.Written,
                published.Removed,
                published.Unchanged,
                Recovered = recovered
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
