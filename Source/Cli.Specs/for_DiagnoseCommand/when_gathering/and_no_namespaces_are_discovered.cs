// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_no_namespaces_are_discovered : given.healthy_services
{
    void Establish()
    {
        _settings.AllNamespaces = true;
        _services.Namespaces.AllNamespaces(Arg.Any<AllNamespacesRequest>()).Returns(
            QueryResult<IEnumerable<NamespaceNamesResponse>>.Success(Guid.Empty, []));
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_pass_without_checking_any_namespace() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_explain_why_no_check_ran() => _data.ChecksCouldNotRun.Single(x => x.Check == "Namespaces").ShouldEqual(new DiagnoseCheckFailure("Namespaces", "store", null, "No namespaces were found to check"));
    [Fact] void should_not_claim_to_have_checked_any_namespace() => _data.Scopes.ShouldBeEmpty();
}
