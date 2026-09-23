// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_known_options_are_matched_case_insensitively : Specification
{
    string[]? _forwarded;

    void Because() => _forwarded = NewCommandArguments.Partition(["cratis", "--DATABASE", "mongodb", "--DRY-RUN"]);

    [Fact] void should_forward_known_options_in_any_casing() => _forwarded!.ShouldContainOnly(
        ["cratis", "--DATABASE", "mongodb", "--DRY-RUN"]);

    [Fact] void should_capture_nothing() => NewCommandArguments.Captured.ShouldBeEmpty();
}
