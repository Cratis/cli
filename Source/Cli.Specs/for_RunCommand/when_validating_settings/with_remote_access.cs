// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_remote_access : Specification
{
    [Fact] void should_default_to_local_access() => new RunSettings().AllowRemote.ShouldBeFalse();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_the_explicit_choice_in_a_semantic_command(bool allowRemote)
    {
        var settings = new RunSettings { AllowRemote = allowRemote };
        settings.Validate().Successful.ShouldBeTrue();
        settings.SemanticCommand().Contains("--allow-remote", StringComparison.Ordinal).ShouldEqual(allowRemote);
    }
}
