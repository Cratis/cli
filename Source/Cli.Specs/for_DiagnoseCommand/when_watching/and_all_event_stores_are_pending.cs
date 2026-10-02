// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_watching;

[Collection(CliSpecsCollection.Name)]
public class and_all_event_stores_are_pending : given.watch_services
{
    void Establish()
    {
        _settings.AllEventStores = true;
        _cancellation.Cancel();
    }

    async Task Because() => _exitCode = await DiagnoseCommand.RunWatch(_services, _settings, _cancellation.Token);

    [Fact] void should_name_all_stores_in_the_pending_header() => _writer.ToString().ShouldContain("all event stores");
    [Fact] void should_name_all_namespaces_in_the_pending_header() => _writer.ToString().ShouldContain("all namespaces");
    [Fact] void should_not_show_any_passed_check() => _writer.ToString().ShouldNotContain("✓");
}
