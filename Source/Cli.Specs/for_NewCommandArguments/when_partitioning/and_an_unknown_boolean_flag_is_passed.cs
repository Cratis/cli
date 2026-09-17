// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_an_unknown_boolean_flag_is_passed : Specification
{
    void Because() => NewCommandArguments.Partition(["cratis", "--SkipTests", "--language", "csharp"]);

    [Fact] void should_capture_the_flag_alone() => NewCommandArguments.Captured.ShouldContainOnly(["--SkipTests"]);
}
