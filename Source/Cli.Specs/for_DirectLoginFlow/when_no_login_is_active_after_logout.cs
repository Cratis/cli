// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow;

public class when_no_login_is_active_after_logout : for_DirectCredentials.given.stored_credentials
{
    Exception _error = null!;
    bool _storeSelected;

    void Establish() => DirectCredentials.Forget(_config, _config.Credentials.Single(entry => entry.Origin == _config.Origin && entry.Tenant == _config.Tenant));

    void Because()
    {
        using var http = new HttpClient();
        _error = Catch.Exception(() => DirectLoginFlow.Active(new CliConfiguration { Direct = _config }, new DirectSettings(), http, _ =>
        {
            _storeSelected = true;
            return Substitute.For<IDirectSecretStore>();
        }));
    }

    [Fact] void should_report_not_logged_in_instead_of_a_missing_issuer() => _error.Message.ShouldEqual(DirectStatusCommand.NotLoggedIn(3));
    [Fact] void should_report_an_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_open_a_credential_store_without_an_active_login() => _storeSelected.ShouldBeFalse();
}
