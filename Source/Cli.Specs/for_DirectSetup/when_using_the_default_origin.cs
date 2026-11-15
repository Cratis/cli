// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_using_the_default_origin : given.a_setup
{
    void Establish() => _settings.Url = null;
    async Task Because() => _result = await Run();
    [Fact] void should_login_to_the_app_host() => _loginSettings.Url.ShouldEqual("https://app.cratis.direct");
    [Fact] void should_default_new_configuration_to_the_app_host() => new DirectConfiguration().Origin.ShouldEqual("https://app.cratis.direct");
}
