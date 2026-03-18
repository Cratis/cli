// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChatCommand;

public class when_checking_write_tools : Specification
{
    [Fact] void should_include_replay_observer() => ChronicleChatTools.WriteToolNames.ShouldContain("replay_observer");
    [Fact] void should_include_replay_partition() => ChronicleChatTools.WriteToolNames.ShouldContain("replay_partition");
    [Fact] void should_include_retry_partition() => ChronicleChatTools.WriteToolNames.ShouldContain("retry_partition");
    [Fact] void should_include_perform_recommendation() => ChronicleChatTools.WriteToolNames.ShouldContain("perform_recommendation");
    [Fact] void should_not_include_list_observers() => ChronicleChatTools.WriteToolNames.ShouldNotContain("list_observers");
}
