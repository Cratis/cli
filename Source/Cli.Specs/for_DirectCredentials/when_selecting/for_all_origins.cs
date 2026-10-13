// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class for_all_origins : given.stored_credentials
{
    IReadOnlyList<DirectCredentialEntry> _selected = null!;

    void Because() => _selected = DirectCredentials.Select(_config, null, null, true);

    [Fact] void should_select_every_stored_credential() => _selected.Count.ShouldEqual(4);
}
