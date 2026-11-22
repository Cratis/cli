// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_a_saved_personal_access_token_is_used : Specification
{
    DirectAuthError _error = null!;

    void Because()
    {
        using var http = new HttpClient();
        _error = Assert.Throws<DirectAuthError>(() => DirectLoginFlow.TokenProviderFor(
            new DirectCredentialEntry { Origin = "https://direct.example", Kind = DirectCredentialEntry.PersonalAccessToken },
            http));
    }

    [Fact] void should_refuse_to_use_it() => _error.ShouldNotBeNull();
    [Fact] void should_tell_the_person_to_sign_in() => _error.Message.ShouldContain("cratis direct login");
}
