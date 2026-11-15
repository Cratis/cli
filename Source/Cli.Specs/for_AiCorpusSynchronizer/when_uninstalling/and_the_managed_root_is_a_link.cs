// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_the_managed_root_is_a_link : given.an_ownership_plan
{
    Exception? _error;
    string _moved = null!;

    void Establish()
    {
        _moved = Path.Combine(_corpus, "saved-ai");
        Directory.Move(Path.Combine(_project, ".cratis", "ai"), _moved);
        Directory.CreateSymbolicLink(Path.Combine(_project, ".cratis", "ai"), _moved);
    }

    void Because() => _error = Catch.Exception(() => AiCorpusSynchronizer.Uninstall(_project, force: true));

    [Fact] void should_reject_the_linked_managed_root() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_managed_root_target() => File.Exists(Path.Combine(_moved, "rules", "general.md")).ShouldBeTrue();
    [Fact] void should_keep_the_manifest() => File.ReadAllText(ManifestPath).ShouldEqual(_originalManifest);
}
