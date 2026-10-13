// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReleaseBaselineFile.when_reading;

public class and_it_is_a_directory : given.a_baseline_file
{
    void Establish() => Directory.CreateDirectory(_path);
    void Because() => _result = ReleaseBaselineFile.Read(_path, _application);

    [Fact] void should_report_unreadable() => _result.Status.ShouldEqual("unreadable");
}
