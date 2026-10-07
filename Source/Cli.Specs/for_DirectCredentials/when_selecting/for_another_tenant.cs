// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class for_another_tenant : given.stored_credentials
{
    IReadOnlyList<DirectCredentialEntry> _selected = null!;

    void Because() => _selected = DirectCredentials.Select(_config, null, "previous", false);

    [Fact] void should_select_that_tenant_on_the_active_origin() => _selected.ShouldContainOnly([Entry("https://direct.example", "previous")]);
}
