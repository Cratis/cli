// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServiceDefinition;

public class when_describing_the_service_of_a_project : Specification
{
    BuildServiceSettings _settings;
    BuildServiceDefinition _definition;
    string _project;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), "My Shop");
        _settings = new() { Project = _project, Agent = "claude", Verify = ["dotnet build"], NoPush = true };
    }

    void Because() => _definition = BuildServiceDefinition.For(_settings, "/usr/local/bin/cratis", [], "/usr/bin:/bin");

    [Fact] void should_run_the_build_loop_in_the_project() =>
        _definition.Arguments.Take(4).ToArray().ShouldEqual(["build", "run", "--project", _project]);
    [Fact] void should_pass_the_agent() => _definition.Arguments.ShouldContain("claude");
    [Fact] void should_pass_the_verification_commands() => _definition.Arguments.ShouldContain("dotnet build");
    [Fact] void should_pass_no_push() => _definition.Arguments.ShouldContain("--no-push");
    [Fact] void should_not_run_once() => _definition.Arguments.ShouldNotContain("--once");
    [Fact] void should_name_the_service_after_the_folder() => _definition.Name.StartsWith("cratis-build-my-shop-", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_service_the_same_for_the_same_project() => BuildServiceDefinition.NameFor(_project).ShouldEqual(_definition.Name);
    [Fact] void should_name_another_project_differently() => BuildServiceDefinition.NameFor(Path.Combine(Path.GetTempPath(), "Other", "My Shop")).ShouldNotEqual(_definition.Name);
}
