// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_an_unknown_option_uses_the_inline_value_form : Specification
{
    string[]? _forwarded;

    void Because() => _forwarded = NewCommandArguments.Partition(["cratis", "--Framework=net10.0", "-n", "App"]);

    [Fact] void should_capture_the_single_token() => NewCommandArguments.Captured.ShouldContainOnly(["--Framework=net10.0"]);

    [Fact] void should_not_consume_the_following_option_as_a_value() =>
        _forwarded!.ShouldContainOnly(["cratis", "-n", "App"]);
}
