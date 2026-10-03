// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectSecretStores;

public class when_selecting_plaintext_storage_on_windows : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => DirectSecretStores.Select(true, "/unused", "windows"));

    [Fact] void should_reject_the_unusable_store_before_authorization() => _error.ShouldBeOfExactType<DirectAuthError>();
}
