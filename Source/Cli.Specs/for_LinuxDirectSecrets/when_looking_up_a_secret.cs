// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_looking_up_a_secret : given.a_fake_secret_tool
{
    string? _secret;

    void Establish() => Behave("printf 'stored-value\\n'\nexit 0");

    async Task Because()
    {
        if (!OperatingSystem.IsWindows())
        {
            _secret = await new LinuxDirectSecrets(_tool).Read("ABC123", CancellationToken.None);
        }
    }

    [Fact] void should_look_up_the_same_attributes_it_stores() => (OperatingSystem.IsWindows() || Arguments.SequenceEqual(["lookup", "application", "cratis-cli", "service", "cratis-direct", "key", "ABC123"])).ShouldBeTrue();
    [Fact] void should_return_the_secret_without_the_trailing_newline() => (OperatingSystem.IsWindows() || _secret == "stored-value").ShouldBeTrue();
}
