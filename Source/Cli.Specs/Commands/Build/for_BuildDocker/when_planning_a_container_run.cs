// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildDocker;

public class when_planning_a_container_run : Specification
{
    IReadOnlyList<string> _arguments;
    IReadOnlyList<string> _inner;

    void Because()
    {
        var settings = new BuildRunSettings { Level = "code-ai", Target = "axon", Agent = "claude", Ai = "studio", Once = true, Verify = ["mvn test"] };
        _arguments = BuildDocker.Arguments(settings, "/src/shop", "cratis/builder:1.0.0", name => name == "ANTHROPIC_API_KEY", "/nonexistent-home", "1000:1000");
        _inner = BuildDocker.Inner(settings);
    }

    [Fact] void should_mount_the_project_as_the_workspace() => _arguments.ShouldContain("/src/shop:/workspace");
    [Fact] void should_take_the_connection_from_the_environment() => _arguments.ShouldContain(StudioConnection.BuilderTokenVariable);
    [Fact] void should_never_put_the_token_on_the_command_line() => string.Join(' ', _arguments).ShouldNotContain("=" + StudioConnection.BuilderTokenVariable);
    [Fact] void should_pass_an_agent_sign_in_that_is_set() => _arguments.ShouldContain("ANTHROPIC_API_KEY");
    [Fact] void should_leave_out_an_agent_sign_in_that_is_not() => _arguments.ShouldNotContain("OPENAI_API_KEY");
    [Fact] void should_run_as_the_given_user() => _arguments.SkipWhile(argument => argument != "--user").Skip(1).First().ShouldEqual("1000:1000");
    [Fact] void should_name_the_image_before_the_command() => _arguments.SkipWhile(argument => argument != "cratis/builder:1.0.0").Skip(1).Take(3).ShouldContainOnly(["build", "run", "--project"]);
    [Fact] void should_run_the_same_builder_inside() => string.Join(' ', _inner).ShouldEqual("build run --project /workspace --agent claude --level code-ai --target axon --ai studio --verify mvn test --once");
    [Fact] void should_default_the_image_to_the_cli_version() => BuildDocker.ImageFor(null, "2.4.1+abc").ShouldEqual("cratis/builder:2.4.1");
    [Fact] void should_use_the_image_it_is_given() => BuildDocker.ImageFor("cratis/studio-axon-builder:3", "2.4.1").ShouldEqual("cratis/studio-axon-builder:3");
}
