// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_marten_and_wolverine_are_present : given.provider_compilations
{
    ProviderSelection _selection = null!;

    void Because() => _selection = new ProviderScreenplayGeneration().Discover(
        LoadedFrom(
            "namespace Marten { public class StoreOptions; } " +
            "namespace Wolverine { public class WolverineOptions; }"),
        "/workspace/Application.csproj");

    [Fact] void should_select_the_more_specific_critter_stack_provider() => _selection.Provider!.Name.ShouldEqual(ScreenplayProviders.CritterStack);
    [Fact] void should_report_no_error() => _selection.Error.ShouldBeNull();
}
