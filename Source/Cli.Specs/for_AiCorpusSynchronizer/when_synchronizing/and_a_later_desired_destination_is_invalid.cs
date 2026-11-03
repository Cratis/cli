// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_later_desired_destination_is_invalid : given.an_ownership_plan
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "NUL.md"), "# Invalid destination");

    void Because()
    {
        for (var operation = 5; operation < 9; operation++)
        {
            _accesses.Clear();
            var operations = new AiFileOperations(operation > 6) { ObserveDestination = (kind, path) => _accesses.Add((kind, path)) };
            _outcomes.Add((Catch.Exception(() => Execute(operation, operations)), _accesses.Count));
        }
    }

    [Fact] void should_reject_every_install_variant() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_not_probe_or_migrate_destinations() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
    [Fact] void should_preserve_the_manifest() => File.ReadAllText(ManifestPath).ShouldEqual(_originalManifest);
}
