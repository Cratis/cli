// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// Factory for the common passphrase sources.
/// </summary>
public static class PassphraseSources
{
    /// <summary>
    /// The environment variable holding the passphrase.
    /// </summary>
    public const string PassphraseVariable = "CRATIS_PASSPHRASE";

    /// <summary>
    /// The environment variable naming a file whose contents are the passphrase.
    /// </summary>
    public const string PassphraseFileVariable = "CRATIS_PASSPHRASE_FILE";

    /// <summary>
    /// The environment variable holding the new passphrase when changing it non-interactively.
    /// </summary>
    public const string NewPassphraseVariable = "CRATIS_NEW_PASSPHRASE";

    /// <summary>
    /// The environment variable naming a file whose contents are the new passphrase.
    /// </summary>
    public const string NewPassphraseFileVariable = "CRATIS_NEW_PASSPHRASE_FILE";

    /// <summary>
    /// Creates a source that reads <c language="csharp">CRATIS_PASSPHRASE</c> or <c language="csharp">CRATIS_PASSPHRASE_FILE</c> and otherwise asks the fallback.
    /// </summary>
    /// <param name="fallback">Optional fallback, typically a prompt. Without one a missing passphrase is an error.</param>
    /// <param name="newFallsBackToCurrent">True when the current passphrase variables also supply a new passphrase (creating an environment); false when changing the passphrase.</param>
    /// <returns>The source.</returns>
    public static IPassphraseSource FromEnvironment(IPassphraseSource? fallback = null, bool newFallsBackToCurrent = true) => new EnvironmentPassphraseSource(fallback, newFallsBackToCurrent);

    /// <summary>
    /// Creates a source for a fixed passphrase, for tests and hosts that already hold one in memory.
    /// </summary>
    /// <param name="passphrase">The passphrase.</param>
    /// <param name="newPassphrase">The new passphrase, defaulting to the same one.</param>
    /// <returns>The source.</returns>
    public static IPassphraseSource Fixed(string passphrase, string? newPassphrase = null) => new FixedPassphraseSource(passphrase, newPassphrase ?? passphrase);

    sealed class FixedPassphraseSource(string passphrase, string newPassphrase) : IPassphraseSource
    {
        public string GetPassphrase() => passphrase;

        public string GetNewPassphrase() => newPassphrase;
    }
}
