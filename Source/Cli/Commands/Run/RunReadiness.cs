// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// What a session reports once it is ready to accept requests.
/// </summary>
/// <param name="Startup">The startup the container reported.</param>
/// <param name="Path">The Screenplay file or folder being run.</param>
/// <param name="Endpoints">The endpoints the session is published on.</param>
/// <param name="Engine">The runtime engine reported by the Stage, if available.</param>
/// <param name="Notes">The non-blocking compiler diagnostics the engine ignored, when reported.</param>
/// <param name="Stack">The plan of the run when the model has authentication, so the services beside the Stage are reported with it.</param>
/// <param name="Container">The name of the container left running when the run was detached, which it is stopped by.</param>
internal sealed record RunReadiness(
    StageStartup Startup,
    string Path,
    StageEndpoints Endpoints,
    string? Engine = null,
    IReadOnlyList<StageRuntimeNote>? Notes = null,
    RunStackPlan? Stack = null,
    string? Container = null);
