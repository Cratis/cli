// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayMcpInvocation.when_serving_a_project_without_a_model;

public class and_the_client_only_reads : given.a_live_mcp_session
{
    JsonElement _opened;
    JsonElement _state;
    int _exitCode;

    async Task Because()
    {
        _opened = Call("open-workspace").GetProperty("structuredContent");
        Call("describe-application");
        _state = Call("workspace-state").GetProperty("structuredContent");
        _exitCode = await Stop();
    }

    [Fact] void should_open_an_empty_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_absent_state() => _state.GetProperty("exists").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_create_a_model_folder() => Directory.Exists(ModelFolder).ShouldBeFalse();
    [Fact] void should_leave_the_repository_as_it_was() => Directory.EnumerateFileSystemEntries(_project).Select(Path.GetFileName).ShouldContainOnly("README.md");
    [Fact] void should_end_the_session_cleanly() => _exitCode.ShouldEqual(ExitCodes.Success);
}
