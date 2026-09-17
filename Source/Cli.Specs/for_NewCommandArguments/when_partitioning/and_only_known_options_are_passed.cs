// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_only_known_options_are_passed : Specification
{
    string[]? _forwarded;

    void Because() => _forwarded = NewCommandArguments.Partition(
        ["cratis", "--language", "csharp", "-n", "App", "--database", "mongodb", "--dry-run"]);

    [Fact] void should_forward_everything() => _forwarded!.ShouldContainOnly(
        ["cratis", "--language", "csharp", "-n", "App", "--database", "mongodb", "--dry-run"]);

    [Fact] void should_capture_nothing() => NewCommandArguments.Captured.ShouldBeEmpty();
}
