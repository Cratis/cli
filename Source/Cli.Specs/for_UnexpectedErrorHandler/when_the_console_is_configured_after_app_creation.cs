// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_the_console_is_configured_after_app_creation : given.an_error_handler
{
    async Task Because()
    {
        var app = CliApp.Create();
        app.Configure(config => config.ConfigureConsole(_console));
        _exitCode = await app.RunAsync(["render", "--workspace"]);
    }

    [Fact] void should_preserve_the_parse_error_exit_code() => _exitCode.ShouldEqual(-1);
    [Fact] void should_write_to_the_configured_console() => _output.ToString().ShouldContain("Option 'workspace' is defined but no value has been provided.");
}
