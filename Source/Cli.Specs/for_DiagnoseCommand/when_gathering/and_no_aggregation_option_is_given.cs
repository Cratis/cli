// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_no_aggregation_option_is_given : given.healthy_services
{
    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_check_only_the_selected_namespace() => _data.Scopes.Select(x => x.Namespace).ShouldContainOnly("tenant-one");
    [Fact] void should_not_discover_other_namespaces() => _services.Namespaces.DidNotReceive().AllNamespaces(Arg.Any<AllNamespacesRequest>());
    [Fact] void should_be_healthy() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
}
