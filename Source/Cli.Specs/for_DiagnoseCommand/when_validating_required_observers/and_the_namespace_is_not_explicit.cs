// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

public class and_the_namespace_is_not_explicit : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    void should_reject_an_implicit_or_blank_namespace(string? ns) => DiagnoseCommand.ValidateRequiredObservers(new DiagnoseSettings
    {
        EventStore = "store",
        Namespace = ns,
        RequireObserver = ["required"]
    }).ShouldEqual("--require-observer requires explicit --event-store and --namespace");
}
