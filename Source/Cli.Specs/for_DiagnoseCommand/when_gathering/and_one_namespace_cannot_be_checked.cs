// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_one_namespace_cannot_be_checked : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.FailedPartitions.GetFailedPartitions(Arg.Is<GetFailedPartitionsRequest>(x => x.Namespace == "tenant-one"))
            .Returns(Task.FromException<IEnumerable<FailedPartition>>(new Exception("Permission denied")));
        _services.FailedPartitions.GetFailedPartitions(Arg.Is<GetFailedPartitionsRequest>(x => x.Namespace == "tenant-two"))
            .Returns(Task.FromResult<IEnumerable<FailedPartition>>([new FailedPartition { ObserverId = "observer", Partition = "partition" }]));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_name_the_unchecked_namespace() => _data.ChecksCouldNotRun.ShouldContainOnly(new DiagnoseCheckFailure("Failed partitions", "store", "tenant-one", "Permission denied"));
    [Fact] void should_still_check_other_namespaces() => _data.Findings.ShouldContainOnly(new DiagnoseFinding("Failed partition", "store", "tenant-two", "observer/partition"));
    [Fact] void should_keep_the_known_failure_count() => _data.FailedPartitions.ShouldEqual(1);
}
