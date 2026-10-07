// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class with_all_and_a_tenant : given.stored_credentials
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => DirectCredentials.Select(_config, null, "previous", true));

    [Fact] void should_refuse_the_ambiguous_request() => _error.ShouldBeOfExactType<DirectAuthError>();
}
