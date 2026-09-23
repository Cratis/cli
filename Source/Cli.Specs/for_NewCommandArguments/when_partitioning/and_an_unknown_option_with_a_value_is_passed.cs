// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_an_unknown_option_with_a_value_is_passed : Specification
{
    string[]? _forwarded;

    void Because() => _forwarded = NewCommandArguments.Partition(
        ["cratis", "--language", "csharp", "--Framework", "net10.0", "-n", "App"]);

    [Fact] void should_forward_the_known_options_only() => _forwarded!.ShouldContainOnly(
        ["cratis", "--language", "csharp", "-n", "App"]);

    [Fact] void should_capture_the_option_and_its_value() =>
        NewCommandArguments.Captured.ShouldContainOnly(["--Framework", "net10.0"]);
}
