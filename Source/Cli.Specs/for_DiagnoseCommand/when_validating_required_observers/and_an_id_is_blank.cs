// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

public class and_an_id_is_blank : Specification
{
    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    void should_reject_blank_ids(string id) => DiagnoseCommand.ValidateRequiredObservers(new DiagnoseSettings
    {
        EventStore = "store",
        Namespace = "tenant-one",
        RequireObserver = ["valid", id]
    }).ShouldEqual("--require-observer IDs must not be blank");
}
