// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ReleaseBaselineFile.when_reading;

public class and_it_exceeds_the_limit : given.a_baseline_file
{
    void Establish()
    {
        using var file = File.Create(_path);
        file.SetLength(ReleaseBaselineStore.MaximumBytes + 1L);
    }

    void Because() => _result = ReleaseBaselineFile.Read(_path, _application);

    [Fact] void should_report_unreadable() => _result.Status.ShouldEqual("unreadable");
    [Fact] void should_explain_the_size_limit() => _result.Reason!.ShouldContain("bounded file size");
}
