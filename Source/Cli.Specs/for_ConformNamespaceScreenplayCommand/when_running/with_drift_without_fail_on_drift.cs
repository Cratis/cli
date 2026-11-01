// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_drift_without_fail_on_drift : given.a_conform_namespace_command
{
    void Establish()
    {
        Appended(1, _register);
        Appended(1, type: "Undeclared");
    }

    async Task Because() => await Run();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_still_report_drift() => Json.GetProperty("hasDrift").GetBoolean().ShouldBeTrue();
}
