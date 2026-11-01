// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_handling_a_parse_error : given.an_error_handler
{
    int _originalExitCode;
    string _originalOutput = string.Empty;

    async Task Because()
    {
        _originalExitCode = await CreateParser(withHandler: false).RunAsync(["missing"]);
        _originalOutput = _output.ToString();
        _output.GetStringBuilder().Clear();
        _exitCode = await CreateParser(withHandler: true).RunAsync(["missing"]);
    }

    CommandApp CreateParser(bool withHandler)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.ConfigureConsole(_console);
            config.AddCommand<Cratis.Cli.Commands.Version.VersionCommand>("version");
            if (withHandler)
            {
                config.SetExceptionHandler((exception, _) => UnexpectedErrorHandler.Handle(exception, ["missing"], _console));
            }
        });
        return app;
    }

    [Fact] void should_preserve_the_exit_code() => _exitCode.ShouldEqual(_originalExitCode);
    [Fact] void should_return_minus_one() => _exitCode.ShouldEqual(-1);
    [Fact] void should_preserve_spectres_rendering() => _output.ToString().ShouldEqual(_originalOutput);
    [Fact] void should_have_rendered_the_parse_error() => _originalOutput.ShouldContain("Error:");
    [Fact] void should_not_treat_a_parse_error_as_an_unexpected_error() => _output.ToString().ShouldNotContain("unexpected_error");
}
