// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_namespace_discovery_fails : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>()).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Error(Guid.Empty, new Exception("Cannot list namespaces")));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_be_healthy_without_checking_any_namespace() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_report_the_failed_discovery() => _data.ChecksCouldNotRun.Single().Check.ShouldEqual("Namespaces");
    [Fact] void should_preserve_the_server_reason() => _data.ChecksCouldNotRun.Single().Reason.ShouldContain("Cannot list namespaces");
    [Fact] void should_not_claim_to_have_checked_any_namespace() => _data.Scopes.ShouldBeEmpty();
}
