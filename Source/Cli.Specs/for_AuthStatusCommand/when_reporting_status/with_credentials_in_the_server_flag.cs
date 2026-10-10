// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_credentials_in_the_server_flag : given.an_auth_status
{
    void Establish() => _settings.Server = "chronicle://reader:flag-secret@other:35000/?apiKey=flag-key";
    async Task Because() => await Execute();

    [Fact] void should_not_show_the_flag_password() => _status.RootElement.GetRawText().ShouldNotContain("flag-secret");
    [Fact] void should_not_show_the_flag_api_key() => _status.RootElement.GetRawText().ShouldNotContain("flag-key");
    [Fact] void should_report_the_mismatch() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeFalse();
}
