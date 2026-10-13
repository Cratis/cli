// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class for_the_active_login : given.stored_credentials
{
    IReadOnlyList<DirectCredentialEntry> _selected = null!;

    void Because() => _selected = DirectCredentials.Select(_config, null, null, false);

    [Fact] void should_select_only_the_active_origin_and_tenant() => _selected.ShouldContainOnly([Entry("https://direct.example", "active")]);
}
