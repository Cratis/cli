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
            return new(count, [Error("CLI-RENDER-017", $"{outcome.FailureKind}: {outcome.Reason}", root)], null);
        }

        var workspace = outcome.Workspace;
        var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
        var documentRequest = new ScreenplayDocumentRenderRequest(
            SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog, workspace.AttachmentContents),
            workspace.ApplicationName,
            request.Target,
            request.ProjectName,
            request.RootNamespace)
        {
            Scope = request.Scope,
            AttachmentDiagnostics = workspace.AttachmentDiagnostics,
            ApprovedPackages = request.ApprovedPackages,
            ReleaseBaseline = request.ReleaseBaseline
        };

        return PlanDocuments(documentRequest, workspace, outcome.ReleaseBaseline, cancellationToken);
    }
}
