// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Answers the first-time publisher question without a console.
/// </summary>
/// <param name="interactive">Whether a person could answer.</param>
/// <param name="answer">What the person answers.</param>
class FixedPublisherPrompt(bool interactive = false, bool answer = false) : IPublisherPrompt
{
    public bool IsInteractive => interactive;

    public int Asked { get; private set; }

    public bool Confirm(string publisher, string? fingerprint, string resolved)
    {
        Asked++;
        return answer;
    }
}
