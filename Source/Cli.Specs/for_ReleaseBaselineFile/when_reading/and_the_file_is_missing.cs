// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReleaseBaselineFile.when_reading;

public class and_the_file_is_missing : given.a_baseline_file
{
    void Because() => _result = ReleaseBaselineFile.Read(_path, _application);

    [Fact] void should_report_absence_for_implicit_discovery() => _result.Status.ShouldEqual("absent");
    [Fact] void should_not_create_the_file() => File.Exists(_path).ShouldBeFalse();
}
