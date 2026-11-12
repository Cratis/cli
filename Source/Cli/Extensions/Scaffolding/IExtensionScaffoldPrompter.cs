// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Asks the person the wizard's questions.
/// </summary>
public interface IExtensionScaffoldPrompter
{
    /// <summary>
    /// Asks for one of several choices.
    /// </summary>
    /// <param name="question">The question.</param>
    /// <param name="choices">The choices, in the order to offer them.</param>
    /// <returns>The chosen value.</returns>
    string Choose(string question, IReadOnlyList<string> choices);

    /// <summary>
    /// Asks for a line of text.
    /// </summary>
    /// <param name="question">The question.</param>
    /// <param name="suggestion">The answer taken when the person gives none, or null when an answer is required.</param>
    /// <returns>The text.</returns>
    string Ask(string question, string? suggestion);
}
