// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// A required observer's health in one event-store and namespace scope.
/// </summary>
/// <param name="ObserverId">The exact required observer ID.</param>
/// <param name="EventStore">The selected event store.</param>
/// <param name="Namespace">The selected namespace.</param>
/// <param name="Status">The requirement outcome.</param>
/// <param name="ObservedState">The observed running state, or null if unavailable or absent.</param>
/// <param name="Reason">The explanation of the outcome.</param>
public record DiagnoseObserverRequirement(string ObserverId, string EventStore, string Namespace, DiagnoseObserverRequirementStatus Status, string? ObservedState, string Reason);

internal static class DiagnoseObserverRequirements
{
    internal static DiagnoseObserverRequirement Evaluate(string observerId, string eventStore, string ns, IReadOnlyList<ObserverInformation>? observers)
    {
        if (observers is null) return new(observerId, eventStore, ns, DiagnoseObserverRequirementStatus.CouldNotCheck, null, "see Could not check");
        var matches = observers.Where(candidate => string.Equals(candidate.Id, observerId, StringComparison.Ordinal)).ToArray();
        if (matches.Length == 0) return new(observerId, eventStore, ns, DiagnoseObserverRequirementStatus.Absent, null, "not registered in store/namespace");
        if (matches.Length > 1) return new(observerId, eventStore, ns, DiagnoseObserverRequirementStatus.Unsatisfied, null, "ambiguous");
        var observer = matches[0];
        var (status, reason) = EvaluateState(observer.RunningState);
        return new(observerId, eventStore, ns, status, observer.RunningState.ToString(), reason);
    }

    static (DiagnoseObserverRequirementStatus Status, string Reason) EvaluateState(ObserverRunningState state) => state switch
    {
        ObserverRunningState.Active => (DiagnoseObserverRequirementStatus.Satisfied, "running"),
        ObserverRunningState.Replaying => (DiagnoseObserverRequirementStatus.Satisfied, "running; replaying"),
        _ => (DiagnoseObserverRequirementStatus.Unsatisfied, UnsatisfiedStateReason(state))
    };

    static string UnsatisfiedStateReason(ObserverRunningState state) => state switch
    {
        ObserverRunningState.Disconnected => "no connected client",
        ObserverRunningState.Suspended => "required observer suspended",
        ObserverRunningState.Quarantined => "quarantined",
        ObserverRunningState.Unknown => "state unknown",
        _ => $"unrecognized state {(int)state}"
    };
}
