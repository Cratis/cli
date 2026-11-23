// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.CheckRelease;
using Cratis.Cli.Registration;

namespace Cratis.Cli.for_CheckReleaseCommand;

public class when_listing_usage_examples : Specification
{
    string[] _examples;
    CliCommandAttribute _command;

    void Because()
    {
        _examples = [.. typeof(CheckReleaseCommand).GetCustomAttributes(typeof(CliExampleAttribute), false).Cast<CliExampleAttribute>().Select(example => string.Join(' ', example.Args))];
        _command = (CliCommandAttribute)typeof(CheckReleaseCommand).GetCustomAttributes(typeof(CliCommandAttribute), false).Single();
    }

    [Fact] void should_be_named_check_release() => _command.Name.ShouldEqual("check-release");
    [Fact] void should_demonstrate_a_release_file() => _examples.ShouldContain("chronicle check-release --release release.json --event-store Shop");
}
