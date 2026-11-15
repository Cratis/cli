// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_an_old_cli_recorded_the_nested_claude_link_as_replaceable : given.an_old_claude_installation
{
    AiStatus _status = null!;
    void Because() => _status = AiCorpusSynchronizer.Status(_project);

    [Fact] void should_accept_the_recorded_layout() => _status.ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_keep_the_record_unchanged() => NestedRecord.PreserveExisting.ShouldBeFalse();
    [Fact] void should_preserve_the_link() => new FileInfo(NestedPath).LinkTarget.ShouldNotBeNull();
}
