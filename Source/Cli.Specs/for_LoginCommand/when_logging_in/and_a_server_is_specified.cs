// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_a_server_is_specified : given.a_login_command
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _settings.Server = "chronicle://override:35001/?skipTlsValidation=true";
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because() => _result = await Execute();

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_call_the_override_server() => _endpoint.RequestUri!.Authority.ShouldEqual("override:35001");
    [Fact] void should_store_the_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldEqual("user-token");
    [Fact] void should_bind_the_token_to_the_override() => CliConfiguration.Load().Contexts["production"].TokenServer.ShouldEqual("override:35001");
    [Fact] void should_store_the_expiry() => DateTimeOffset.Parse(CliConfiguration.Load().Contexts["production"].TokenExpiry!).ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(50));
    [Fact] void should_leave_the_context_server_unchanged() => CliConfiguration.Load().Contexts["production"].Server.ShouldEqual("chronicle://production:35000");
    [Fact] void should_not_print_a_note_for_json() => _error.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_send_the_token_to_the_context_server() => new ChronicleSettings().ResolveConnectionString().ShouldNotContain("apiKey=");
    [Fact] void should_use_the_development_client_for_the_context_server() => new ChronicleSettings().ResolveConnectionString().ShouldContain(ChronicleConnectionString.DevelopmentClient);
    [Fact] void should_send_the_token_with_a_matching_override() => new ChronicleSettings { Server = "chronicle://override:35001" }.ResolveConnectionString().ShouldContain("apiKey=user-token");
}
