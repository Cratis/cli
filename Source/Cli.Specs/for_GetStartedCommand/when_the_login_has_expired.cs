// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.GetStarted;

namespace Cratis.Cli.for_GetStartedCommand;

[Collection(CliSpecsCollection.Name)]
public class when_the_login_has_expired : given.a_temp_config_directory
{
    int _result;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new()
                {
                    Server = "chronicle://production:35000",
                    LoggedInUser = "admin",
                    AccessToken = "expired",
                    TokenExpiry = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"),
                    TokenServer = "production:35000"
                }
            }
        }.Save();
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because()
    {
        _result = await ((ICommand<ChronicleSettings>)new GetStartedCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "get-started", null),
            new ChronicleSettings { Output = OutputFormats.JsonCompact },
            CancellationToken.None);
        Console.SetError(_previousError);
    }

    [Fact] void should_return_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_report_login_expired() => _error.ToString().ShouldContain("Login expired");
}
