// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_an_old_cli_recorded_the_nested_claude_link_as_replaceable : given.an_old_claude_installation
{
    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project);

    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_the_owned_link() => new FileInfo(NestedPath).LinkTarget.ShouldBeNull();
    [Fact] void should_remove_the_manifest() => File.Exists(Path.Combine(_project, ".cratis", "ai.manifest.json")).ShouldBeFalse();
}
