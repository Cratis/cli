// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class for_all_on_one_origin : given.stored_credentials
{
    IReadOnlyList<DirectCredentialEntry> _selected = null!;

    void Because() => _selected = DirectCredentials.Select(_config, "https://other.example", null, true);

    [Fact] void should_select_only_that_origin() => _selected.ShouldContainOnly([Entry("https://other.example", "active")]);
}
