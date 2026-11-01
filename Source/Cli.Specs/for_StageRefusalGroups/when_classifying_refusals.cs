// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRefusalGroups;

public class when_classifying_refusals : Specification
{
    [Theory]
    [InlineData("Vibration/SavedViews/ReviseExplorerView.play(78,5): Specification 'given readmodel' for 'SavedView' is missing required property 'plantId'; supply it in the example or step.", "Fix your model")]
    [InlineData("Control/Dispatch/ControlRequestsAwaitingDispatch.play(53,5): Slice 'ControlRequestsAwaitingDispatch' uses event 'ControlRequestDispatched' produced by slice 'PublishControlRequest' drawn after it. Consider drawing the producer before the consumer.", "Fix your model")]
    [InlineData("Control/Requests/ControlLog.play(78,5): Timeline group feature 'Requests', feature 'Dispatch' depend on each other's events or read models; reordering these members cannot make every dependency flow left to right.", "Fix your model")]
    [InlineData("Shared/access.play(53,1): Persona 'PlantEngineer' is authoring metadata and is not part of ESM v1 behavior.", "Ignored by this engine")]
    [InlineData("Shared/access.play(72,1): Authentication providers are realization metadata and are not part of ESM v1 behavior.", "Ignored by this engine")]
    [InlineData("Telemetry/seed.play(4,1): Event seeding is operational metadata and is not part of ESM v1 behavior.", "Ignored by this engine")]
    [InlineData("Vibration/SavedViews/MySavedViews.play(56,3): Screen 'Vibration' is explicitly deferred from the backend ESM v1 profile.", "Ignored by this engine")]
    [InlineData("Telemetry/Spectra/BearingSpectra.play(33,3): Read model 'BearingSpectra' has a composite key, which is not admitted into the executable model.", "Not supported by this engine yet")]
    [InlineData("SourceLocation { Line = 13, Column = 5, Path = Vibration/SavedViews/RenameSavedView.play }: STAGE-ESM-034: Command 'RenameSavedView' reads 'SavedView' with legacy semantics that cannot imply decision consistency.", "Not supported by this engine yet")]
    [InlineData("IS MISSING REQUIRED PROPERTY 'id'; authoring metadata", "Fix your model")]
    void should_classify_each_refusal_kind(string details, string category) =>
        StageRefusalGroups.Create([new("Plan", "model", details)]).Single().Category.ShouldEqual(category);
}
