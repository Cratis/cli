// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Describes the outcome of a destructive-operation confirmation request.
/// </summary>
public enum ConfirmationOutcome
{
    /// <summary>
    /// The operation was explicitly confirmed.
    /// </summary>
    Confirmed = 0,

    /// <summary>
    /// An interactive user declined the operation.
    /// </summary>
    Declined = 1,

    /// <summary>
    /// The operation requires explicit confirmation because no interactive user is available.
    /// </summary>
    ConfirmationRequired = 2
}
