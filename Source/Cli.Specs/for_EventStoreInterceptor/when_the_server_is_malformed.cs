// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.given;

namespace Cratis.Cli.for_EventStoreInterceptor;

[Collection(CliSpecsCollection.Name)]
public class when_the_server_is_malformed : a_temp_config_directory
{
    Exception? _error;
    StringWriter _output = null!;
    TextWriter _previousError = null!;
    EventStoreSettings _settings = null!;

    void Establish()
    {
        _settings = new EventStoreSettings { Server = "not-a-chronicle-url", Output = OutputFormats.JsonCompact };
        _previousError = Console.Error;
        _output = new StringWriter();
        Console.SetError(_output);
    }

    void Because() => _error = Catch.Exception(() => new when_the_login_has_expired.InteractiveInterceptor().Intercept(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null), _settings));

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

    [Fact] void should_not_throw_from_the_interceptor() => _error.ShouldBeNull();
    [Fact] void should_report_a_json_validation_error() => _output.ToString().ShouldContain($"\"error\":\"{ExitCodes.ValidationErrorCode}\"");
    [Fact] void should_mark_the_error_as_reported() => _settings.ConnectionResolutionReported.ShouldBeTrue();
}
