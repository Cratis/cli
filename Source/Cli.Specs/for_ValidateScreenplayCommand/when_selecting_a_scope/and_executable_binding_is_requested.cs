// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_executable_binding_is_requested : given.a_scoped_cli_process
{
    async Task Because() => await Run("--scope", "M.F.Clean", "--executable");

    [Fact] void should_reject_the_incompatible_verdicts() => _exitCode.ShouldEqual(OperatingSystem.IsWindows() ? -1 : 255);
    [Fact] void should_direct_the_caller_to_whole_application_binding() => string.Join(' ', _output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ShouldContain("cannot be combined with --executable");
    [Fact] void should_not_report_a_successful_summary() => _output.ShouldNotContain("\"valid\":true");
}
