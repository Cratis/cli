// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.when_writing;

public class quietly : given.captured_profiles_output
{
    void Because() => Write(OutputFormats.Quiet);

    [Fact] void should_write_only_profile_ids() => _output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldEqual(["cratis/documentation", "cratis/engineering/csharp"]);
}
