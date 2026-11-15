// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageSettings.when_building_arguments;

public class with_repeated_keys : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = StageSettings.BuildArguments(["Sandbox:Users=false", "Other:Value=1", "sandbox:users=true"]);

    [Fact] void should_keep_one_entry_per_key() => _result.Count(argument => argument.StartsWith("Stage__Sandbox__Users=", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
    [Fact] void should_use_the_last_value() => _result.ShouldContain("Stage__sandbox__users=true");
    [Fact] void should_keep_other_keys() => _result.ShouldContain("Stage__Other__Value=1");
}
