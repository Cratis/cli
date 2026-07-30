// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Represents how far the Stage container has come from being launched to being able to accept requests.
/// The phases only ever move forward — output matching an earlier phase never moves the startup back.
/// </summary>
public enum StagePhase
{
    /// <summary>
    /// Docker has been asked to run the container, which has not reported anything yet.
    /// </summary>
    Starting = 0,

    /// <summary>
    /// The Stage image is not present locally and Docker is pulling it.
    /// </summary>
    Pulling = 1,

    /// <summary>
    /// The Chronicle kernel bundled in the container is starting.
    /// </summary>
    StartingChronicle = 2,

    /// <summary>
    /// The Screenplay files in the mounted folder are being compiled into an event model.
    /// </summary>
    CompilingEventModel = 3,

    /// <summary>
    /// The Stage host is starting and exposing the compiled event model as an API.
    /// </summary>
    StartingStage = 4,

    /// <summary>
    /// The Stage host has started and is registering the model's read models and projections with Chronicle.
    /// </summary>
    RegisteringReadModels = 5,

    /// <summary>
    /// Everything the container starts has reported in.
    /// </summary>
    Running = 6
}
