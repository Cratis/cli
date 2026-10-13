// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectFileSecrets;

public class when_writing_on_windows : Specification
{
    Exception? _error;

    async Task Because()
    {
        if (OperatingSystem.IsWindows())
        {
            _error = await Catch.Exception(() => new DirectFileSecrets("unused").Write("key", "secret", CancellationToken.None));
        }
    }

    [Fact] void should_reject_plaintext_without_writing_a_file() => (OperatingSystem.IsWindows() ? _error is DirectAuthError : _error is null).ShouldBeTrue();
}
