// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Namespaces;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_no_aggregation_option_is_given : given.healthy_services
{
    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_not_duplicate_the_tail_in_plain() => _outputs[OutputFormats.Plain].ShouldNotContain("scope_event_sequence_tail=");
    [Fact] void should_preserve_the_top_level_plain_tail() => _outputs[OutputFormats.Plain].ShouldContain("event_sequence_tail=10");
    [Fact] void should_preserve_the_single_scope_in_json() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("scopes").GetArrayLength().ShouldEqual(1);
    [Fact] void should_preserve_the_scoped_json_tail() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("scopes")[0].GetProperty("eventSequenceTail").GetUInt64().ShouldEqual(10UL);
    [Fact] void should_check_only_the_selected_namespace() => _data.Scopes.Select(x => x.Namespace).ShouldContainOnly("tenant-one");
    [Fact] void should_not_discover_other_namespaces() => _services.Namespaces.DidNotReceive().AllNamespaces(Arg.Any<AllNamespacesRequest>());
    [Fact] void should_be_healthy() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
}
