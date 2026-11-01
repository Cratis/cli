// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_linked_workspace_metadata : given.a_project
{
    string _elsewhere;
    Exception _error;

    void Establish()
    {
        _elsewhere = Path.Combine(Path.GetTempPath(), $"cratis-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_elsewhere);
        File.WriteAllText(Path.Combine(_elsewhere, "identities.json"), "{}");
        Directory.CreateSymbolicLink(Path.Combine(_project, ".screenplay"), _elsewhere);
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_linked_metadata() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();

    void Destroy() => Directory.Delete(_elsewhere, recursive: true);
}
