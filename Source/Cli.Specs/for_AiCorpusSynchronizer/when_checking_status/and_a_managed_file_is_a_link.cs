// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_a_managed_file_is_a_link : given.an_ownership_plan
{
    string _sentinel = null!;

    void Establish()
    {
        _sentinel = Path.Combine(_corpus, "sentinel.txt");
        File.WriteAllText(_sentinel, "keep");
        File.Delete(Installed("general.md"));
        File.CreateSymbolicLink(Installed("general.md"), _sentinel);
    }

    void Because() => ExerciseAllCommands();

    [Fact] void should_reject_the_linked_payload_for_every_operation() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_only_inspect_metadata() => _accesses.TrueForAll(access => access.Kind == "metadata").ShouldBeTrue();
    [Fact] void should_preserve_the_referent() => File.ReadAllText(_sentinel).ShouldEqual("keep");
    [Fact] void should_preserve_the_manifest() => File.ReadAllText(ManifestPath).ShouldEqual(_originalManifest);
}
