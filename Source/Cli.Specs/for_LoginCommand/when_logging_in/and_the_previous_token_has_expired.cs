// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_previous_token_has_expired : given.a_login_command
{
    int _result;

    void Establish()
    {
        var config = CliConfiguration.Load();
        var context = config.Contexts["production"];
        context.LoggedInUser = "previous-user";
        context.AccessToken = "old-token";
        context.TokenExpiry = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O");
        config.Save();
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_allow_the_user_to_log_in_again() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_replace_the_expired_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldEqual("user-token");
}
