// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Maps workspace identities and render selections into a destination-independent planning request.
/// </summary>
internal static class RenderWorkspaceRequest
{
    internal static ScreenplayDocumentRenderRequest Create(ScreenplayWorkspace workspace, RenderSettings settings, string target)
    {
        var documents = workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text));
        return new(SemanticDocumentSet.Create([.. documents], workspace.IdentityCatalog), workspace.ApplicationName, target, settings.ProjectName, settings.RootNamespace)
        {
            Strict = settings.Strict,
            RepositoryRoot = settings.RepositoryRoot,
            Scope = settings.Scope,
            MigrateReducers = settings.MigrateReducer
        };
    }
}
