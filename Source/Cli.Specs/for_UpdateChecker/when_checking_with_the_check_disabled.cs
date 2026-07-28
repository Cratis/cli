// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker;

public class when_checking_with_the_check_disabled : Specification
{
    string? _original;
    string? _result;

    void Establish()
    {
        _original = Environment.GetEnvironmentVariable(UpdateChecker.DisableEnvVar);
        Environment.SetEnvironmentVariable(UpdateChecker.DisableEnvVar, "1");
    }

    async Task Because() => _result = await UpdateChecker.CheckForUpdate("Cratis.Cli", "0.0.1");

    void Destroy() => Environment.SetEnvironmentVariable(UpdateChecker.DisableEnvVar, _original);

    [Fact] void should_not_report_an_update_even_though_the_current_version_is_ancient() => _result.ShouldBeNull();
    [Fact] void should_report_itself_as_disabled() => UpdateChecker.IsDisabled().ShouldBeTrue();
}
