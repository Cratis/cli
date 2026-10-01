// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Auth.when_using_a_stored_login_token.context;

namespace Cratis.Cli.Integration.Chronicle.for_Auth;

[Collection(ChronicleCollection.Name)]
public class when_using_a_stored_login_token(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public string Username = $"login-token-{Guid.NewGuid():N}";
        public CliCommandResult AddResult = null!;
        public CliCommandResult LoginResult = null!;
        public CliCommandResult ReadResult = null!;
        public CliCommandResult LogoutResult = null!;
        public CliCommandResult RemoveResult = null!;
        public Exception? LogoutError;
        public Exception? RemoveError;
        public string? UserId;

        async Task Because()
        {
            AddResult = await RunCliAsync("chronicle", "users", "add", Username, $"{Username}@test.com", "TestP@ss123!");

            try
            {
                var user = await WaitForElementInList(
                    $"User '{Username}'",
                    candidate => candidate.TryGetProperty("username", out var username) && username.GetString() == Username,
                    "chronicle",
                    "users",
                    "list");
                UserId = user.GetProperty("id").GetString();

                LoginResult = await RunCliWithoutCredentialsAsync("chronicle", "login", Username, "--secret", "TestP@ss123!");

                // A missing or ignored login token must not pass through the development-client fallback.
                if (LoginResult.ExitCode == ExitCodes.Success)
                {
                    var config = CliConfiguration.Load();
                    var activeContext = config.GetCurrentContext();
                    activeContext.ClientId = "invalid-client";
                    activeContext.ClientSecret = "invalid-secret";
                    config.Save();
                }

                ReadResult = await RunCliWithoutCredentialsAsync("chronicle", "users", "list");
            }
            finally
            {
                try
                {
                    LogoutResult = await CliCommandRunner.RunAsync("chronicle", "logout", "--output", "json");
                }
                catch (Exception ex)
                {
                    LogoutError = ex;
                }

                try
                {
                    if (UserId is not null)
                    {
                        RemoveResult = await RunCliAsync("chronicle", "users", "remove", UserId, "--yes");
                    }
                }
                catch (Exception ex)
                {
                    RemoveError = ex;
                }
            }
        }
    }

    [Fact] void should_add_the_user() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_log_in_without_embedded_credentials() => Context.LoginResult.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_read_using_the_stored_token() => Context.ReadResult.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_find_the_user_with_the_stored_token() => Context.ReadResult.StandardOutput.ShouldContain(Context.Username);
    [Fact] void should_log_out() => Context.LogoutResult.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_remove_the_user() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_fail_during_logout() => Context.LogoutError.ShouldBeNull();
    [Fact] void should_not_fail_during_removal() => Context.RemoveError.ShouldBeNull();
}
