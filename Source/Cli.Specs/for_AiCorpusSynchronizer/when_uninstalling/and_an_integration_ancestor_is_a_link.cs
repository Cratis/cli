// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_an_integration_ancestor_is_a_link : given.an_ownership_plan
{
    Exception? _error;
    string _moved = null!;

    void Establish()
    {
        _moved = Path.Combine(_corpus, "saved-pi");
        Directory.Move(Path.Combine(_project, ".pi"), _moved);
        Directory.CreateSymbolicLink(Path.Combine(_project, ".pi"), _moved);
    }

    void Because() => _error = Catch.Exception(() => AiCorpusSynchronizer.Uninstall(_project, force: true));

    [Fact] void should_reject_the_linked_ancestor() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_ancestor_target() => new DirectoryInfo(Path.Combine(_moved, "skills")).LinkTarget.ShouldEqual("../.cratis/ai/skills");
    [Fact] void should_keep_the_manifest() => File.ReadAllText(ManifestPath).ShouldEqual(_originalManifest);
}
