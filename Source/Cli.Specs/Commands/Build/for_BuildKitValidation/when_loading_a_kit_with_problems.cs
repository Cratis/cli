// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitValidation;

public class when_loading_a_kit_with_problems : given.a_kit_folder
{
    InvalidBuildKit _error;

    void Establish()
    {
        Write("build-kit.yaml", Manifest
            .Replace("name: shell-demo", "name: Shell Demo\nsurprise: true", StringComparison.Ordinal)
            .Replace("[stateChange, stateView]", "[stateChange, everything]", StringComparison.Ordinal)
            .Replace("instructions: ./AGENTS.md", "instructions: ../outside.md", StringComparison.Ordinal)
            .Replace("paths: [\"src/**\"]", "paths: [\"/etc/**\"]", StringComparison.Ordinal));
        Write("build-slice.md", "Build {slice} with {secret}.");
        Directory.CreateDirectory(Path.Combine(_kit, "skills", "empty-skill", "notes"));
        File.WriteAllText(Path.Combine(_kit, "skills", "empty-skill", "notes", "x.md"), "x");
        File.Delete(Path.Combine(_kit, "learn.md"));
    }

    void Because() => _error = (InvalidBuildKit)Catch.Exception(() => BuildKit.Load(_kit))!;

    [Fact] void should_refuse_an_undeclared_member() => _error.Problems.ShouldContain("/surprise: the manifest format does not declare this member.");
    [Fact] void should_refuse_a_name_that_is_not_lowercase_words() => _error.Problems.Any(problem => problem.StartsWith("/name:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_slice_type() => _error.Problems.Any(problem => problem.StartsWith("/sliceTypes/1:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_a_path_leaving_the_kit() => _error.Problems.ShouldContain("/instructions: must start with ./ and never leave the kit.");
    [Fact] void should_refuse_a_missing_file() => _error.Problems.ShouldContain("/existingCodebase/learn: ./learn.md is not in the kit, or is empty.");
    [Fact] void should_refuse_a_skill_without_its_skill_file() => _error.Problems.ShouldContain("/skills: the skill empty-skill has no SKILL.md.");
    [Fact] void should_refuse_a_placeholder_a_host_does_not_fill() => _error.Problems.Any(problem => problem.StartsWith("/prompt: {secret}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_a_permitted_path_outside_the_project() => _error.Problems.ShouldContain("/permissions/paths/0: must be relative to the project and never leave it.");
}
