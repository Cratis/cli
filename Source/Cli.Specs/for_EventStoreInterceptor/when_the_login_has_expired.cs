// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.given;
using Spectre.Console;

namespace Cratis.Cli.for_EventStoreInterceptor;

[Collection(CliSpecsCollection.Name)]
public class when_the_login_has_expired : a_temp_config_directory
{
    Exception? _error;
    StringWriter _output = null!;
    IAnsiConsole _previousConsole = null!;

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
        _previousConsole = AnsiConsole.Console;
        _output = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_output), Ansi = AnsiSupport.No });
        AnsiConsole.Console.Profile.Width = 200;
    }

    void Because()
    {
        _error = Catch.Exception(() => new InteractiveInterceptor().Intercept(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new EventStoreSettings { Output = OutputFormats.Plain }));
        AnsiConsole.Console = _previousConsole;
    }

    [Fact] void should_not_throw_from_the_interceptor() => _error.ShouldBeNull();
    [Fact] void should_report_login_expired_without_json() => _output.ToString().ShouldContain("Login expired");
    [Fact] void should_tell_the_user_to_log_in() => _output.ToString().ShouldContain("cratis chronicle login");

    /// <summary>
    /// Runs the interceptor's interactive path without requiring a real terminal.
    /// </summary>
    public sealed class InteractiveInterceptor : EventStoreInterceptor
    {
        /// <inheritdoc/>
        protected override bool IsInteractive() => true;
    }
}
