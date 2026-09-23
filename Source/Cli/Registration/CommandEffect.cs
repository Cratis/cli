// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Registration;

/// <summary>
/// Describes the strongest state change a command can make when it runs.
/// </summary>
/// <remarks>
/// Every command declares one of these with <see cref="CommandEffectAttribute"/>, and <c language="csharp">cratis llm-context</c>
/// publishes it as the command's <c language="json">effect</c> so agents and guards can tell observing commands from state-changing
/// ones without keeping their own lists. The serialized names are a stable contract; the numeric values are not.
/// Incidental caches the CLI maintains for itself - authentication tokens, update checks, downloaded AI corpus
/// sources, and the first-run default event store prompt - do not count as an effect.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CommandEffect>))]
public enum CommandEffect
{
    /// <summary>
    /// Observes only. Reads from the Chronicle server, an application, or local files and changes nothing.
    /// </summary>
    [JsonStringEnumMemberName("read-only")]
    ReadOnly = 0,

    /// <summary>
    /// Changes only the local machine: CLI configuration and contexts, cached credentials, files in the working
    /// directory, shell configuration, installed tools, or local containers. Never changes server or store state.
    /// </summary>
    [JsonStringEnumMemberName("local")]
    Local = 1,

    /// <summary>
    /// Changes state on a Chronicle server or store without removing or resetting existing state, for example adding
    /// a user, stopping or resuming a job, or retrying a failed partition.
    /// </summary>
    [JsonStringEnumMemberName("mutating")]
    Mutating = 2,

    /// <summary>
    /// Removes or resets state on a Chronicle server or store, for example removing a user or replaying an observer,
    /// which rebuilds the state it has produced.
    /// </summary>
    [JsonStringEnumMemberName("destructive")]
    Destructive = 3,
}
