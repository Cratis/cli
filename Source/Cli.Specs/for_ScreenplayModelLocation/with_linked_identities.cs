// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_linked_identities : given.a_project
{
    Exception _error;

    string _elsewhere;

    void Establish()
    {
        _elsewhere = Path.Combine(Path.GetTempPath(), $"cratis-identities-{Guid.NewGuid():N}.json");
        File.WriteAllText(_elsewhere, "{}");
        Directory.CreateDirectory(Path.Combine(_project, ".screenplay"));
        File.CreateSymbolicLink(Path.Combine(_project, ".screenplay", "identities.json"), _elsewhere);
        Play("Models/application.play");
    }

    void Destroy() => File.Delete(_elsewhere);

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_linked_identities() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();
}
