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

        async Task Because()
        {
            AddResult = await RunCliAsync("chronicle", "users", "add", Username, $"{Username}@test.com", "TestP@ss123!");

            try
            {
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
                LogoutResult = await CliCommandRunner.RunAsync("chronicle", "logout", "--output", "json");
                var listResult = await RunCliAsync("chronicle", "users", "list");
                if (listResult.ExitCode == ExitCodes.Success)
                {
                    using var users = JsonDocument.Parse(listResult.StandardOutput);
                    var user = users.RootElement.EnumerateArray()
                        .FirstOrDefault(candidate => candidate.GetProperty("username").GetString() == Username);
                    if (user.ValueKind != JsonValueKind.Undefined)
                    {
                        RemoveResult = await RunCliAsync("chronicle", "users", "remove", user.GetProperty("id").GetString()!, "--yes");
                    }
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
}
