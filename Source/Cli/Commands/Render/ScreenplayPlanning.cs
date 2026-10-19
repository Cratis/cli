// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Compiles one Screenplay document set and plans all target artifacts before publication.
/// </summary>
/// <param name="compiler">The semantic document-set compiler.</param>
/// <param name="targets">The static reviewed renderer-target roster.</param>
internal sealed partial class ScreenplayPlanning(
    ISemanticModelCompiler compiler,
    RenderTargetRoster targets) : IScreenplayPlanning
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayPlanning"/> class with the shipped compiler and targets.
    /// </summary>
    public ScreenplayPlanning()
        : this(new SemanticModelCompiler(), new RenderTargetRoster())
    {
    }

    /// <inheritdoc/>
    public async Task<ScreenplayRenderPlan> Plan(ScreenplayRenderRequest request, CancellationToken cancellationToken)
    {
        var files = Files(request.SourcePath);
        if (files.Count == 0)
        {
            return new(0, [], null);
        }

        if (!targets.TryGet(request.Target, out _))
        {
            return new(files.Count, [Error("CLI-RENDER-001", $"Renderer target '{request.Target}' is not bundled with this CLI.", null)], null);
        }

        var root = File.Exists(request.SourcePath) ? Path.GetDirectoryName(Path.GetFullPath(request.SourcePath))! : request.SourcePath;
        if (File.Exists(Path.Combine(root, ".screenplay", "identities.json")))
        {
            return PlanModelRoot(root, files.Count, request, cancellationToken);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(request.ApplicationName));
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var source = await File.ReadAllTextAsync(file, cancellationToken);

            // A legacy file has no persisted document identity. Bootstrap an opaque key from its
            // portable relative path; never pass directory separators to the non-path key contract.
            var key = $"file-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(relativePath.Normalize(NormalizationForm.FormC))))}";
            documents.Add(SemanticSourceDocument.Create(
                catalog.ResolveDocument(key),
                key,
                relativePath,
                source));
        }

        var attachments = AttachmentFiles.Load(root, [.. documents]);
        var documentRequest = new ScreenplayDocumentRenderRequest(
            SemanticDocumentSet.Create([.. documents], catalog, attachments.Contents),
            request.ApplicationName,
            request.Target,
            request.ProjectName,
            request.RootNamespace)
        {
            Destination = request.Destination,
            Scope = request.Scope,
            MigrateReducers = request.MigrateReducers,
            AttachmentDiagnostics = attachments.Diagnostics,
            ApprovedPackages = request.ApprovedPackages,
            ReleaseBaseline = request.ReleaseBaseline ?? new UnpersistedModelRootReleaseBaseline(root)
        };
        return PlanDocuments(documentRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public ScreenplayRenderPlan PlanDocuments(ScreenplayDocumentRenderRequest request, CancellationToken cancellationToken) =>
        PlanDocuments(request, null, null, cancellationToken);

    internal ScreenplayRenderPlan PlanDocuments(ScreenplayDocumentRenderRequest request, ScreenplayWorkspace? workspace, ReleaseBaselineRead? baseline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = request.Documents.Documents.Length;
        if (!targets.TryGet(request.Target, out var target))
        {
            return new(count, [Error("CLI-RENDER-001", $"Renderer target '{request.Target}' is not bundled with this CLI.", null)], null);
        }

        var compilation = workspace?.Compilation ?? compiler.Compile(request.ApplicationName, request.Documents);
        var diagnostics = compilation.Diagnostics.Select(Map).ToList();
        if (workspace is null)
        {
            diagnostics.AddRange(request.AttachmentDiagnostics.Select(Map));
        }
        if (!compilation.Success)
        {
            return new(count, diagnostics, null);
        }

        var addresses = SemanticAddressIndex.From(compilation.Value!.Model.Application, request.Scope.Length > 0);
        var selection = RenderSelection.Resolve(request.Scope, addresses, diagnostics);
        if (diagnostics.Exists(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return new(count, diagnostics, null);
        }

        var scene = CompileScene(request.Documents, diagnostics, request.Scene);
        if (scene is not null)
        {
            diagnostics.AddRange(SceneProfileResolution.Check(scene, request.ApprovedPackages));
        }

        if (diagnostics.Exists(_ => _.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return new(count, diagnostics, null);
        }

        if (RenderedSemanticVersions.Check(compilation.Value!.Model.SemanticVersion) is { } notAdmitted)
        {
            diagnostics.Add(notAdmitted);
            return new(count, diagnostics, null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var selectedSlices = selection is null
            ? []
            : addresses.Slices.Where(slice => selection.Contains(slice.Id.ToString(), addresses)).Select(slice => slice.Id).ToArray();
        var execution = selection is null
            ? SemanticExecutionPlan.Compile(compilation.Value.Model)
            : SemanticExecutionPlan.Compile(compilation.Value.Model, selectedSlices);
        diagnostics.AddRange(execution.Issues.Select(issue =>
            Error($"PLAN-{issue.Kind.ToString().ToUpperInvariant()}", issue.Details, issue.Artifact.ToString())));
        diagnostics.AddRange(execution.UnselectedIssues.Select(issue => new ScreenplayDiagnostic(
            ScreenplayDiagnosticSeverity.Warning,
            $"PLAN-{issue.Kind.ToString().ToUpperInvariant()}",
            issue.Details,
            addresses.Resolve(issue.Artifact.ToString()).Address?.Replace('/', '.') ?? issue.Artifact.ToString())));
        if (!execution.Success)
        {
            return new(count, diagnostics, null);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var convention = target is CratisRenderTarget
                ? CratisReducerConvention.Resolve(request.ApplicationName, request.Destination, new(request.ProjectName ?? request.ApplicationName, request.RootNamespace ?? request.ApplicationName))
                : default;
            if (convention.Error is { } conventionError)
            {
                diagnostics.Add(conventionError);
                return new(count, diagnostics, null);
            }

            var migration = request.MigrateReducers.Length == 0 || (target is CratisRenderTarget && request.Scope.Length == 0)
                ? CratisReducerMigration.Resolve(request.MigrateReducers, request.Destination, addresses)
                : ([], Error(
                    CratisReducerMigration.RefusalCode,
                    target is CratisRenderTarget
                        ? "--migrate-reducer persists the reducer configuration, so it needs a whole-application render; remove --scope."
                        : $"Renderer target '{request.Target}' does not support --migrate-reducer.",
                    null));
            if (migration.Error is { } migrationError)
            {
                diagnostics.Add(migrationError);
                return new(count, diagnostics, null);
            }

            var contents = RenderImplementationBodies.Resolve(request.Documents, compilation.ImplementationRequirements);
            var releaseBaseline = request.ReleaseBaseline?.Assess(compilation.Value) ??
                (workspace is not null && baseline is { Status: not "absent" }
                    ? StageReleaseBaseline.Check(workspace, baseline)
                    : ReleaseBaselineAssessment.NotSupplied);
            var artifacts = target!.Plan(
                compilation.Value,
                execution.Plan!,
                request.ProjectName,
                request.RootNamespace,
                compilation.ImplementationRequirements,
                contents,
                compilation.TypedContextDescriptors,
                request.AttachmentDiagnostics,
                diagnostics,
                scene,
                selection,
                request.ApprovedPackages,
                releaseBaseline,
                convention.Input,
                migration.Evidence);
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.AddRange(artifacts.Diagnostics.Select(Map));
            return new(count, diagnostics, artifacts)
            {
                SemanticAddresses = addresses,
                Selection = selection,
                ScaffoldOptions = ScaffoldOptionsFor(compilation.Value.Model.Application.Name, request, convention.Input)
            };
        }
        catch (InvalidCratisBackendApplicationScaffold exception)
        {
            diagnostics.Add(Error("CLI-RENDER-002", exception.Message, null));
            return new(count, diagnostics, null);
        }
        catch (InvalidSemanticContract exception)
        {
            diagnostics.Add(Error("CLI-RENDER-005", $"The bundled renderer violated its contract. Nothing was planned or published. Report this to Cratis/Stage with this message: {exception.Message}", null));
            return new(count, diagnostics, null);
        }
        catch (InvalidArtifactRenderContract exception)
        {
            diagnostics.Add(Error("CLI-RENDER-005", exception.Message, null));
            return new(count, diagnostics, null);
        }
    }

    static CratisPlanOptions ScaffoldOptionsFor(string applicationName, ScreenplayDocumentRenderRequest request, ArtifactRenderInput? renderingInput)
    {
        var options = new CratisPlanOptions(applicationName, request.ProjectName ?? applicationName, request.RootNamespace ?? applicationName);
        if (request.ApprovedPackages.IsEmpty && renderingInput is null)
        {
            return options;
        }

        var profile = CratisRendering.CreateProfile(options.ApplicationName, new(options.ProjectName, options.RootNamespace), null, null, null, request.ApprovedPackages);
        return options with { Profile = renderingInput is null ? profile : CratisRendering.WithReducerConvention(profile, renderingInput) };
    }

    static SceneApplication? CompileScene(SemanticDocumentSet documents, List<ScreenplayDiagnostic> diagnostics, SceneApplication? providedScene)
    {
        var compilation = new PlayFileCompiler(new DocumentSetPlayFiles(documents), new ScreenplayCompiler()).CompileFolder(".");
        diagnostics.AddRange(compilation.Result.Diagnostics.Select(Map));
        if (!compilation.Result.Success)
        {
            return null;
        }

        if (diagnostics.Exists(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return null;
        }

        if (providedScene is not null)
        {
            return providedScene;
        }

        var scene = new ScreenplaySceneVisitor().Visit(compilation.Result.Value!);
        return scene.Screens.Count > 0 ? scene : null;
    }

    static IReadOnlyList<string> Files(string path) => File.Exists(path)
        ? [Path.GetFullPath(path)]
        : [.. Directory.EnumerateFiles(path, $"*{PlayFileTargetResolver.Extension}", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Order(StringComparer.Ordinal)];

    static ScreenplayDiagnostic Map(Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            $"{diagnostic.Location.Path ?? "Screenplay"}({diagnostic.Location.Line},{diagnostic.Location.Column})");

    static ScreenplayDiagnostic Map(ArtifactRenderDiagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Artifact.IsSet ? diagnostic.Artifact.ToString() : null);

    static ScreenplayDiagnostic Error(string code, string message, string? location) =>
        new(ScreenplayDiagnosticSeverity.Error, code, message, location);
}

sealed class DocumentSetPlayFiles(SemanticDocumentSet documents) : IPlayFiles
{
    readonly Dictionary<string, SemanticSourceDocument> _documents = documents.Documents.ToDictionary(
        document => document.DisplayPath,
        StringComparer.Ordinal);

    public IEnumerable<PlayFile> FindIn(string root) => _documents.Values
        .OrderBy(document => document.DisplayPath, StringComparer.Ordinal)
        .Select(document => new PlayFile(Path.Combine(root, document.DisplayPath), document.DisplayPath));

    public string ReadContent(PlayFile file) => _documents[file.RelativePath].Text;
}
