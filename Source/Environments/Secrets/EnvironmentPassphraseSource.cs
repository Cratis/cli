// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// Reads the passphrase from <c language="csharp">CRATIS_PASSPHRASE</c> or <c language="csharp">CRATIS_PASSPHRASE_FILE</c> and otherwise asks a fallback.
/// </summary>
/// <param name="fallback">The optional fallback.</param>
/// <param name="newFallsBackToCurrent">True when creating an environment, where <c language="csharp">CRATIS_PASSPHRASE</c> is also the new passphrase; false when changing it, where the new one must be given explicitly.</param>
public class EnvironmentPassphraseSource(IPassphraseSource? fallback = null, bool newFallsBackToCurrent = true) : IPassphraseSource
{
    /// <inheritdoc/>
    public string GetPassphrase() =>
        Read(PassphraseSources.PassphraseVariable, PassphraseSources.PassphraseFileVariable) ??
        fallback?.GetPassphrase() ??
        throw new EnvironmentException($"No passphrase available. Set {PassphraseSources.PassphraseVariable} or {PassphraseSources.PassphraseFileVariable}, or run interactively.");

    /// <inheritdoc/>
    public string GetNewPassphrase() =>
        Read(PassphraseSources.NewPassphraseVariable, PassphraseSources.NewPassphraseFileVariable) ??
        (newFallsBackToCurrent ? Read(PassphraseSources.PassphraseVariable, PassphraseSources.PassphraseFileVariable) : null) ??
        fallback?.GetNewPassphrase() ??
        throw new EnvironmentException($"No new passphrase available. Set {PassphraseSources.NewPassphraseVariable} or {PassphraseSources.NewPassphraseFileVariable}, or run interactively.");

    static string? Read(string variable, string fileVariable)
    {
        var direct = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(direct))
        {
            return direct;
        }

        var file = Environment.GetEnvironmentVariable(fileVariable);
        if (string.IsNullOrEmpty(file))
        {
            return null;
        }

        if (!File.Exists(file))
        {
            throw new EnvironmentException($"{fileVariable} points to a file that does not exist: {file}");
        }

        return File.ReadAllText(file).TrimEnd('\r', '\n');
    }
}
