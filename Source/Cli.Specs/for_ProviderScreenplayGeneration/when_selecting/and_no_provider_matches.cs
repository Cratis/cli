// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_no_provider_matches : given.provider_compilations
{
    ProviderSelection _selection = null!;

    void Because() => _selection = new ProviderScreenplayGeneration().Discover(
        LoadedFrom("public class Application;"),
        "/workspace/Application.csproj");

    [Fact] void should_select_no_provider() => _selection.Provider.ShouldBeNull();
    [Fact] void should_report_no_match() => _selection.Error!.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.NoMatchingProvider);
    [Fact] void should_list_available_providers() => _selection.Error!.Diagnostics.Single().Message.ShouldContain("arc, critter-stack, marten");
}
