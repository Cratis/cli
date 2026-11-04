// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_a_personal_access_token_is_active : Specification
{
    DirectConfiguration _config = null!;

    void Establish() => _config = JsonSerializer.Deserialize<DirectConfiguration>("""
        {"Origin":"https://direct.example","Credentials":[{"Origin":"https://direct.example","Kind":"personal-access-token"}]}
        """) ?? throw new InvalidOperationException("Selection fixture was invalid.");

    [Fact] void should_have_an_active_selection_without_an_issuer() => _config.HasActiveSelection.ShouldBeTrue();
    [Fact] void should_select_it_for_logout() => DirectCredentials.Select(_config, null, null, false).Count.ShouldEqual(1);
}
