// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The state of a board prompt the session can take.
/// </summary>
public enum ModelingPromptState
{
    /// <summary>A person handed the agent a message.</summary>
    Queued = 0,

    /// <summary>A person confirmed what the agent proposed.</summary>
    Confirmed = 3,

    /// <summary>A person declined what the agent proposed.</summary>
    Declined = 4
}
