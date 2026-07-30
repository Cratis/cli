// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.when_observing_output;

public class and_the_container_reports_an_error : given.a_stage_startup
{
    void Because() => _startup.Observe("ERROR: No Screenplay .play files found under /eventmodel/");

    [Fact] void should_capture_the_error_without_the_prefix() => _startup.Error.ShouldEqual("No Screenplay .play files found under /eventmodel/");
}
