// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_marten_and_wolverine_are_in_separate_projects : given.provider_compilations
{
    ProviderSelection _selection = null!;

    void Because() => _selection = new ProviderScreenplayGeneration().Discover(
        LoadedFromProjects(
            ("Persistence", "namespace Marten { public class StoreOptions; }"),
            ("Worker", "namespace Wolverine { public class WolverineOptions; }")),
        "/workspace/Application.slnx");

    [Fact] void should_select_critter_stack_for_the_application_scope() => _selection.Provider!.Name.ShouldEqual(ScreenplayProviders.CritterStack);
    [Fact] void should_report_no_error() => _selection.Error.ShouldBeNull();
}
