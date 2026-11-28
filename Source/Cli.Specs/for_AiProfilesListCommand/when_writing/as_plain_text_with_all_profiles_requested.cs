// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class as_plain_text_with_all_profiles_requested : given.captured_featured_profiles_output
{
    void Because() => Write(OutputFormats.Plain, all: true);

    [Fact] void should_list_the_featured_profile() => _output.ShouldContain("cratis/application\tpublic\t-\t1");
    [Fact] void should_list_the_other_profiles() => _output.ShouldContain("cratis/application/csharp\tpublic\tcsharp\t0");
}
