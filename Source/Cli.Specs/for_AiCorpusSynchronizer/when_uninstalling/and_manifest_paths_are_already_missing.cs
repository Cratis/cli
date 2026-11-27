// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_manifest_paths_are_already_missing : given.an_installation_with_agents_and_prompts
{
    void Establish()
    {
        File.Delete(Path.Combine(_project, ".cratis", "ai", "agents", "screenplay-modeler.md"));
        File.Delete(Path.Combine(_project, ".pi", "prompts", "example.md"));
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project, force: true);

    [Fact] void should_uninstall_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_the_preexisting_empty_asset_directory() => Directory.Exists(Path.Combine(_project, ".cratis", "ai", "agents")).ShouldBeTrue();
    [Fact] void should_keep_the_preexisting_empty_integration_directory() => Directory.Exists(Path.Combine(_project, ".pi", "prompts")).ShouldBeTrue();
    [Fact] void should_remove_the_remaining_files() => File.Exists(Installed("general.md")).ShouldBeFalse();
}
