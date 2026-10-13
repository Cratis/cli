// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_secret_tool_is_missing : Specification
{
    Exception _error = null!;

    async Task Because() => _error = await Catch.Exception(() => new LinuxDirectSecrets(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "secret-tool")).Write("ABC123", "secret", CancellationToken.None));

    [Fact] void should_fail_with_an_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_say_how_to_install_it() => _error.Message.ShouldContain("libsecret-tools");
}
