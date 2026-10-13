// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectSecretStores;

public class when_selecting_a_store_with_plaintext_consent : Specification
{
    IDirectSecretStore _store = null!;

    void Because() => _store = DirectSecretStores.Select(true, "/unused", "linux");

    [Fact] void should_choose_the_file_store() => _store.ShouldBeOfExactType<DirectFileSecrets>();
}
