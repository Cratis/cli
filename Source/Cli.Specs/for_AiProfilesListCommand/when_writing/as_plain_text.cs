// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class as_plain_text : given.captured_profiles_output
{
    void Because() => Write(OutputFormats.Plain);

    [Fact] void should_write_the_header() => _output.ShouldContain("Profile\tCatalog\tLanguages\tComposes");
    [Fact] void should_write_the_engineering_profile_row() => _output.ShouldContain("cratis/engineering/csharp\tengineering\tcsharp\t1");
    [Fact] void should_mark_missing_languages_with_a_dash() => _output.ShouldContain("cratis/documentation\tpublic\t-\t0");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
