// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_old_cli_recorded_the_nested_claude_link_as_replaceable : given.an_old_claude_installation
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_record_the_link_as_preserved() => NestedRecord.PreserveExisting.ShouldBeTrue();
    [Fact] void should_keep_the_link() => new FileInfo(NestedPath).LinkTarget.ShouldNotBeNull();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_accept_the_updated_manifest() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
