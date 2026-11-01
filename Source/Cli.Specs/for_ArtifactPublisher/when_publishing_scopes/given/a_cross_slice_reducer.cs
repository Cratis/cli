// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes.given;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class a_cross_slice_reducer : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    protected string _reducerPath = null!;
    protected byte[] _reducerBytes = null!;
    protected IReadOnlyList<IReadOnlyList<string>> _owners = null!;
    private protected ScreenplayRenderPlan _full = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(_lookupFile, """
            module Projects
              feature Registration
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    name String
                  query ProjectById => ProjectSummary optional
                    by projectId ProjectId
                  reducer FoldSummary => ProjectSummary
                    on ProjectRegistered
                      ```csharp
                      return new ProjectSummary(ProjectId: context.Event.ProjectId, Name: context.Event.Name);
                      ```
            """);
        _full = await Plan();
        Assert.True(_full.Success, string.Join("; ", _full.Diagnostics.Select(diagnostic => diagnostic.Message)));
        await Publish(_full);
        _reducerPath = _full.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/FoldSummary.cs", StringComparison.Ordinal)).RelativePath;
        _reducerBytes = await File.ReadAllBytesAsync(Path.Combine(_destination, _reducerPath));
        _owners = ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Single(artifact => artifact.Path == _reducerPath).Owners!;
    }
}
