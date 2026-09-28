// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_returns_a_token : given.a_login_command
{
    int _result;
    CliConfiguration _config = null!;

    async Task Because()
    {
        _result = await Execute();
        _config = CliConfiguration.Load();
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_persist_the_token_in_the_active_context() => _config.Contexts["production"].AccessToken.ShouldEqual("user-token");
    [Fact] void should_persist_the_expiry() => DateTimeOffset.Parse(_config.Contexts["production"].TokenExpiry!).ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(50));
    [Fact] void should_bind_the_token_to_the_server() => _config.Contexts["production"].TokenServer.ShouldEqual("production:35000");
    [Fact] void should_record_the_user() => _config.Contexts["production"].LoggedInUser.ShouldEqual("admin");
    [Fact] void should_clear_the_previous_client_credential() => _config.Contexts["production"].ClientId.ShouldBeNull();
    [Fact] void should_leave_other_contexts_alone() => _config.Contexts["development"].AccessToken.ShouldBeNull();
    [Fact] void should_use_the_token_on_subsequent_commands() => new ChronicleSettings().ResolveConnectionString().ShouldContain("apiKey=user-token");
    [Fact] void should_use_the_token_with_a_matching_server_override() => new ChronicleSettings { Server = "chronicle://PRODUCTION" }.ResolveConnectionString().ShouldContain("apiKey=user-token");
    [Fact] void should_not_use_the_development_client() => new ChronicleSettings().ResolveConnectionString().ShouldNotContain("chronicle-dev-client");
    [Fact] void should_call_the_context_server() => _endpoint.RequestUri!.Host.ShouldEqual("production");
    [Fact] void should_restrict_configuration_file_permissions_on_unix()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.GetUnixFileMode(CliConfiguration.GetConfigPath()).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
