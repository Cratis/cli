// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_confirmation_is_canceled : Specification
{
    Exception _error = null!;

    async Task Because() => _error = await Catch.Exception(() => DirectBrowser.Complete("validated-code", () => throw new OperationCanceledException()));

    [Fact] void should_preserve_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
}
