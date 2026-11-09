// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The agent a modeling session hands board prompts to, with the session it was started with.
/// </summary>
/// <param name="Agent">The agent.</param>
/// <param name="Session">Its session, whose permission to change the board the modeling session sets per prompt.</param>
public sealed record ModelingAgent(IBuildAgent Agent, BuildAgentSession Session);
