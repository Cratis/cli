// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// One agent session: a coding agent, or a generator, the host hands prompts to. A session may take several prompts, as a
/// modeling session does.
/// </summary>
public interface IBuildAgent : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the agent, for people.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Hands the agent a prompt and waits until it is done.
    /// </summary>
    /// <param name="prompt">The prompt.</param>
    /// <param name="environment">Environment values for a generator, such as where the order is.</param>
    /// <param name="cancellationToken">Stops the agent.</param>
    /// <returns>What came of it.</returns>
    Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken);
}
