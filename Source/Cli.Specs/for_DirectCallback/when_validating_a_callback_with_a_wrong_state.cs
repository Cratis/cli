// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCallback;

public class when_validating_a_callback_with_a_wrong_state : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => DirectCallback.Validate(new Uri("http://127.0.0.1:34123/callback"), new Uri("http://127.0.0.1:34123/callback?code=one&state=wrong&iss=https%3A%2F%2Fidentity.example.com%2F"), "random", new Uri("https://identity.example.com/")));

    [Fact] void should_reject_the_state() => _error.ShouldBeOfExactType<DirectAuthError>();
}
