// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_arc_and_critter_stack_are_present : given.provider_compilations
{
    ProviderSelection _selection = null!;

    void Because() => _selection = new ProviderScreenplayGeneration().Discover(
        LoadedFrom(
            "namespace Cratis.Arc.Commands.ModelBound { public class CommandAttribute : System.Attribute; } " +
            "namespace Marten { public class StoreOptions; } " +
            "namespace Wolverine { public class WolverineOptions; }"),
        "/workspace/Application.csproj");

    [Fact] void should_select_no_provider() => _selection.Provider.ShouldBeNull();
    [Fact] void should_report_ambiguity() => _selection.Error!.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousProviders);
    [Fact] void should_name_both_candidates() => _selection.Error!.Diagnostics.Single().Message.ShouldContain("arc, critter-stack");
}
