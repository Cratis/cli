// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

[Collection(nameof(CapturedArgumentsCollection))]
public class and_help_is_requested : Specification
{
    string[]? _forwarded;

    void Because() => _forwarded = NewCommandArguments.Partition(["--help", "--Framework", "net10.0"]);

    [Fact] void should_forward_help_to_the_framework() => _forwarded!.ShouldContain("--help");

    [Fact] void should_still_capture_the_template_argument() =>
        NewCommandArguments.Captured.ShouldContainOnly(["--Framework", "net10.0"]);
}
