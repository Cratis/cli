// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_no_profile_selection_is_given : given.captured_install_output
{
    void Because() => Write(OutputFormats.Json);

    [Fact] void should_only_include_existing_warnings() => Warnings().ShouldContainOnly("Existing warning");
}
