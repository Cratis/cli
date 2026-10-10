// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_not_selected : given.captured_install_output
{
    void Because() => Write(OutputFormats.Json, ["cratis", "cratis/full"]);

    [Fact] void should_only_include_existing_warnings() => Warnings().ShouldContainOnly("Existing warning");
}
