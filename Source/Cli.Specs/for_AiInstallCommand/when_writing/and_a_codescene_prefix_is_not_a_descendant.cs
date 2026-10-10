// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_a_codescene_prefix_is_not_a_descendant : given.captured_install_output
{
    void Because() => Write(OutputFormats.Json, ["cratis/codescene-other"]);

    [Fact] void should_only_include_existing_warnings() => Warnings().ShouldContainOnly("Existing warning");
}
