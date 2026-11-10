// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitInstaller;

public class when_the_project_is_not_empty_and_the_kit_cannot_learn_it : given.a_kit_and_a_project
{
    Exception _error;

    void Establish()
    {
        Write("build-kit.yaml", string.Join('\n', Manifest.Split('\n').Where(line => !line.StartsWith("existingCodebase", StringComparison.Ordinal) && !line.StartsWith("  learn", StringComparison.Ordinal) && !line.StartsWith("  conventions", StringComparison.Ordinal))));
        File.WriteAllText(Path.Combine(_project, "Program.cs"), "// an existing codebase");
    }

    async Task Because() => _error = await Catch.Exception(() => Install("codex"));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidBuildKit>();
    [Fact] void should_say_the_kit_does_not_build_into_an_existing_codebase() => ((InvalidBuildKit)_error).Problems.Single().StartsWith("/existingCodebase", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_record_a_kit() => File.Exists(Path.Combine(_project, BuildProjectSettings.File)).ShouldBeFalse();
    [Fact] void should_not_write_instructions() => File.Exists(Path.Combine(_project, "AGENTS.md")).ShouldBeFalse();
}
