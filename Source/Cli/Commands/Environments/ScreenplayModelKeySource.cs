// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Reads the keys a Screenplay model needs from its declarations: <c language="text">authentication.providers.&lt;Identity&gt;.*</c> for every identity provider and
/// <c language="text">onboarding.email.*</c> when the model declares onboarding, <c language="text">secrets.&lt;Name&gt;</c> for every named secret and <c language="text">systems.&lt;System&gt;.&lt;setting&gt;</c> for each setting of an extension a system does not map. The convention is Screenplay's (<c language="text">EnvironmentRequirements</c>); this only asks it.
/// </summary>
public class ScreenplayModelKeySource : IModelKeySource
{
    /// <inheritdoc/>
    public bool CanRead(string path) =>
        (File.Exists(path) && string.Equals(Path.GetExtension(path), RunInput.Extension, StringComparison.OrdinalIgnoreCase)) ||
        Run.PlayFiles.ExistIn(path);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown when the model does not compile, since the keys of a model that is not understood cannot be known.</exception>
    public IReadOnlyList<string> ReadKeys(string path) =>
        [.. ModelRequirements.Read(path).Requirements.Keys.Select(key => key.Key).Distinct(StringComparer.Ordinal)];
}
