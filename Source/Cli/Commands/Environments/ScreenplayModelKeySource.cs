// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Files;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Reads the keys a Screenplay model needs from its declarations: <c language="text">authentication.providers.&lt;Identity&gt;.*</c> for every identity provider and
/// <c language="text">onboarding.email.*</c> when the model declares onboarding. The convention is Screenplay's (<c language="text">EnvironmentRequirements</c>); this only asks it.
/// </summary>
public class ScreenplayModelKeySource : IModelKeySource
{
    /// <inheritdoc/>
    public bool CanRead(string path) =>
        (File.Exists(path) && string.Equals(Path.GetExtension(path), RunInput.Extension, StringComparison.OrdinalIgnoreCase)) ||
        Run.PlayFiles.ExistIn(path);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown when the model does not compile, since the keys of a model that is not understood cannot be known.</exception>
    public IReadOnlyList<string> ReadKeys(string path)
    {
        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(path) ? compiler.CompileFolder(path).Result : compiler.CompileFile(path).Result;
        if (!result.Success || result.Value is null)
        {
            var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Take(3).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}");
            throw new InvalidOperationException($"The model at '{path}' does not compile, so the keys it needs cannot be listed. {string.Join(' ', errors)}".Trim());
        }

        return [.. EnvironmentRequirements.For(result.Value).Keys.Select(key => key.Key).Distinct(StringComparer.Ordinal)];
    }
}
