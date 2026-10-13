// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_storing_a_secret : given.a_fake_secret_tool
{
    const string Secret = "{\"RefreshToken\":\"very-secret\"}";

    void Establish() => Behave("exit 0");

    async Task Because()
    {
        if (!OperatingSystem.IsWindows())
        {
            await new LinuxDirectSecrets(_tool).Write("ABC123", Secret, CancellationToken.None);
        }
    }

    [Fact] void should_pass_attribute_value_pairs_that_identify_the_item() => (OperatingSystem.IsWindows() || Arguments.SequenceEqual(["store", "--label=Cratis Direct", "application", "cratis-cli", "service", "cratis-direct", "key", "ABC123"])).ShouldBeTrue();
    [Fact] void should_send_the_secret_through_standard_input() => (OperatingSystem.IsWindows() || Input == Secret).ShouldBeTrue();
    [Fact] void should_not_put_the_secret_in_the_arguments() => Arguments.Any(argument => argument.Contains("very-secret", StringComparison.Ordinal)).ShouldBeFalse();
}
