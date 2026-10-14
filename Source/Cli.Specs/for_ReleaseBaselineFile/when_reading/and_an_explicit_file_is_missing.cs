// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReleaseBaselineFile.when_reading;

public class and_an_explicit_file_is_missing : given.a_baseline_file
{
    void Because() => _result = ReleaseBaselineFile.Read(_path, _application, required: true);

    [Fact] void should_report_unreadable() => _result.Status.ShouldEqual("unreadable");
    [Fact] void should_explain_the_missing_file() => _result.Reason.ShouldEqual($"the release baseline file '{_path}' does not exist");
}
