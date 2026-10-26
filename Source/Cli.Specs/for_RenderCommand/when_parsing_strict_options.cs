// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand;

public class when_parsing_strict_options : for_CliApp.given.a_cli_process
{
    readonly string _missingSource = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.play");
    readonly string _destination = Path.Combine(Path.GetTempPath(), $"strict-check-{Guid.NewGuid():N}");

    async Task Because() => await Run("render", _missingSource, "--strict", "--check", "--name", "MyApp", "--destination", _destination, "--output", OutputFormats.JsonCompact);

    [Fact] void should_parse_strict_and_check_before_reporting_missing_input() => _exitCode.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_report_the_existing_error_not_strict_rejection() => _error.ShouldContain(ExitCodes.NotFoundCode);
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
