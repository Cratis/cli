// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectSecretStores;

public class when_selecting_a_store_on_windows : Specification
{
    IDirectSecretStore _store = null!;

    void Because() => _store = DirectSecretStores.Select(false, "/unused", "windows");

    [Fact] void should_choose_credential_manager() => _store.ShouldBeOfExactType<WindowsDirectSecrets>();
}
