// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Frames a board prompt for the agent: what it is asked, whether it may change the board, and whether its answer is a
/// proposal waiting for a person.
/// </summary>
public static class ModelingPrompts
{
    /// <summary>
    /// Frames a prompt.
    /// </summary>
    /// <param name="prompt">The board prompt.</param>
    /// <param name="eventModelId">The board the agent works on.</param>
    /// <param name="unattended">Whether the agent acts without asking.</param>
    /// <returns>The text for the agent, whether it may change the board, and whether its answer is a proposal.</returns>
    public static (string Text, bool MayChange, bool IsProposal) For(ModelingPrompt prompt, string eventModelId, bool unattended)
    {
        var (text, mayChange, isProposal) = Framed(prompt, unattended);
        return ($"You work on the Studio event model board {eventModelId} (its eventModelId), through Studio's tools. {text}", mayChange, isProposal);
    }

    static (string Text, bool MayChange, bool IsProposal) Framed(ModelingPrompt prompt, bool unattended) => prompt.State switch
    {
        ModelingPromptState.Confirmed => (
            $"The person confirmed your proposal for: \"{prompt.Text}\". Carry it out on the board now with Studio's tools, then say in a few sentences what you changed.",
            true,
            false),
        ModelingPromptState.Declined => (
            $"The person declined your proposal for: \"{prompt.Text}\". Change nothing. Acknowledge it in one or two sentences and ask what they want instead.",
            false,
            false),
        _ when unattended => (
            $"A person on the Studio board asked you: \"{prompt.Text}\". Do it on the board with Studio's tools, then say in a few sentences what you changed. Your answer is posted in the board chat.",
            true,
            false),
        _ => (
            $"A person on the Studio board asked you: \"{prompt.Text}\". Read the board with Studio's tools and propose what you would change, as a short list. Do not change the board yet: the person confirms or declines your proposal first. Your answer is posted in the board chat.",
            false,
            true)
    };
}
