// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageSettings.when_validating;

public class with_invalid_settings : Specification
{
    [Theory]
    [InlineData("=true")]
    [InlineData("Sandbox:Users")]
    [InlineData("--privileged=true")]
    [InlineData("Sandbox::Users=true")]
    [InlineData("Sandbox:Users;other=true")]
    [InlineData("Sandbox:Users=hello\nworld")]
    [InlineData("Sandbox:Users=hello\0world")]
    void should_reject_malformed_settings(string setting) => StageSettings.Error([setting]).ShouldNotBeNull();

    [Fact] void should_allow_empty_values() => StageSettings.BuildArguments(["Sandbox:Users="]).ShouldContain("Stage__Sandbox__Users=");
    [Fact] void should_allow_no_settings() => StageSettings.BuildArguments([]).ShouldBeEmpty();
    [Fact] void should_reject_invalid_settings_during_cli_validation() => new RunSettings { StageSettings = ["--privileged=true"] }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_reconstruct_the_option_for_the_semantic_command() => new RunSettings { StageSettings = ["Sandbox:Users=true"] }.SemanticCommand().ShouldContain("--stage-setting \"Sandbox:Users=true\"");
}
