// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class as_plain_text_with_featured_profiles : given.captured_featured_profiles_output
{
    void Because() => Write(OutputFormats.Plain);

    [Fact] void should_describe_the_featured_profile() => _output.ShouldContain("cratis/application\tBuild an application on Cratis.");
    [Fact] void should_leave_out_the_other_profiles() => _output.ShouldNotContain("cratis/application/csharp");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
