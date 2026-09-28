// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_reading_the_installed_revision;

public class and_nothing_is_installed : given.a_project
{
    string? _result;

    void Because() => _result = AiUpdateCheck.InstalledRevision(_project);

    [Fact] void should_report_nothing_installed() => _result.ShouldBeNull();
}
