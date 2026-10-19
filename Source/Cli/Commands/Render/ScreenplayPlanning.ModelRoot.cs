// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Render;

internal sealed partial class ScreenplayPlanning
{
    ScreenplayRenderPlan PlanModelRoot(string root, int count, ScreenplayRenderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(root);
        cancellationToken.ThrowIfCancellationRequested();
        if (!outcome.Success)
        {
            var reason = outcome.Reason ?? string.Empty;
            var message = reason.StartsWith($"{outcome.FailureKind}:", StringComparison.Ordinal) ? reason : $"{outcome.FailureKind}: {reason}";
            return new(count, [Error("CLI-RENDER-017", message, root)], null);
        }

        var workspace = outcome.Workspace;
        var savedName = workspace.ApplicationName;
        if (!string.Equals(request.ApplicationName, savedName, StringComparison.Ordinal))
        {
            return new(
                count,
                [Error("CLI-RENDER-018", $"--name '{request.ApplicationName}' does not match the model root's saved application name '{savedName}'. Use --name {savedName}; --project-name and --root-namespace change generated names without changing the application identity.", root)],
                null);
        }
        var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
        var documentRequest = new ScreenplayDocumentRenderRequest(
            SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog, workspace.AttachmentContents),
            workspace.ApplicationName,
            request.Target,
            request.ProjectName,
            request.RootNamespace)
        {
            Destination = request.Destination,
            Scope = request.Scope,
            MigrateReducers = request.MigrateReducers,
            AttachmentDiagnostics = workspace.AttachmentDiagnostics,
            ApprovedPackages = request.ApprovedPackages,
            ReleaseBaseline = request.ReleaseBaseline
        };

        return PlanDocuments(documentRequest, workspace, outcome.ReleaseBaseline, cancellationToken);
    }
}
