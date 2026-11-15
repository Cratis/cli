// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// The outcome of checking a required observer in the selected scope.
/// </summary>
public enum DiagnoseObserverRequirementStatus
{
    /// <summary>The observer is active or replaying.</summary>
    Satisfied,

    /// <summary>The observer is not registered in the selected scope.</summary>
    Absent,

    /// <summary>The observer is registered but is not running.</summary>
    Unsatisfied,

    /// <summary>The observer check was unavailable.</summary>
    CouldNotCheck
}
