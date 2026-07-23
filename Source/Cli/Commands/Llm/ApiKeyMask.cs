// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Masks API keys for display, revealing only enough to identify the key.
/// </summary>
public static class ApiKeyMask
{
    const int VisibleCharacters = 4;
    const string FullMask = "********";

    /// <summary>
    /// Masks an API key, revealing the first and last four characters for keys long enough
    /// to keep the rest secret. Shorter keys are fully masked.
    /// </summary>
    /// <param name="apiKey">The API key to mask.</param>
    /// <returns>The masked representation of the key.</returns>
    public static string Mask(string apiKey) =>
        apiKey.Length > VisibleCharacters * 2
            ? $"{apiKey[..VisibleCharacters]}...{apiKey[^VisibleCharacters..]}"
            : FullMask;
}
