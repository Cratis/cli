// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_all_namespaces_are_selected : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns(Task.FromResult<IEnumerable<FailedPartition>>(
        [
            new FailedPartition { ObserverId = "observer", Partition = "partition" }
        ]));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_aggregate_failed_partitions() => _data.FailedPartitions.ShouldEqual(2);
    [Fact] void should_name_each_findings_namespace() => _data.Findings.ShouldContainOnly(
        new DiagnoseFinding("Failed partition", "store", "tenant-one", "observer/partition"),
        new DiagnoseFinding("Failed partition", "store", "tenant-two", "observer/partition"));
    [Fact] void should_check_each_namespace() => _data.Scopes.Select(x => x.Namespace).ShouldContainOnly("tenant-one", "tenant-two");
    [Fact] void should_not_be_healthy() => _data.IsHealthy.ShouldBeFalse();
}
