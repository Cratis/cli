// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_a_codescene_descendant_is_selected : given.captured_install_output
{
    void Because() => Write(OutputFormats.JsonCompact, ["cratis/codescene/child"]);

    [Fact] void should_include_the_registration_note() => Warnings().Any(warning => warning.StartsWith("CodeScene:", StringComparison.Ordinal)).ShouldBeTrue();
}
