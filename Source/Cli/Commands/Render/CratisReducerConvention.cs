// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Resolves the destination's reducer convention once, without changing application files.
/// </summary>
internal static class CratisReducerConvention
{
    /// <summary>
    /// Reads a saved convention, preserves an existing classic application, or selects a new application's convention.
    /// </summary>
    /// <param name="applicationName">The exact semantic application name.</param>
    /// <param name="destination">The publication destination, or null for destination-independent planning.</param>
    /// <param name="options">The generated project and namespace choices.</param>
    /// <returns>The resolved input or a blocking diagnostic; a null input preserves classic rendering.</returns>
    public static (ArtifactRenderInput? Input, ScreenplayDiagnostic? Error) Resolve(string applicationName, string? destination, CratisRenderingOptions options)
    {
        if (destination is null)
        {
            return (null, null);
        }

        try
        {
            ArtifactPublicationStorage.EnsureSafePath(destination, ArtifactPublicationStorage.ManifestFileName);
            ArtifactPublicationStorage.EnsureSafePath(destination, ReducerConventionInput.RelativePath);
            var manifest = ArtifactPublicationStorage.ReadManifest(destination);
            var owned = manifest?.Artifacts.SingleOrDefault(artifact => artifact.Path == ReducerConventionInput.RelativePath);
            var path = ArtifactPublicationStorage.ArtifactPath(destination, ReducerConventionInput.RelativePath);
            if (File.Exists(path))
            {
                if (manifest is not null && owned is null)
                {
                    return Refused("CLI-RENDER-024", "An existing rendered application has a reducer configuration it did not render. Remove .cratis/stage/rendering.json; an existing application keeps its reducer convention, and changing it is not supported.");
                }

                var input = ReducerConventionInput.Read(applicationName, File.ReadAllText(path));
                if (owned is not null && !string.Equals(ArtifactPublicationStorage.Hash(path), owned.Sha256, StringComparison.Ordinal))
                {
                    return Refused("CLI-RENDER-023", "The managed reducer configuration was edited. Restore its original contents; changing the reducer convention of an existing application is not supported, including with --force.");
                }

                return (input, null);
            }

            if (owned is not null)
            {
                return Refused("CLI-RENDER-022", "The manifest owns the reducer configuration but the file is missing. Restore .cratis/stage/rendering.json from the last render; deleting it cannot migrate an application's reducer convention.");
            }

            return (manifest is null
                ? CratisRendering.CreateNewApplicationProfile(applicationName, options).Inputs.Single(input => input.Name == ReducerConventionInput.Name)
                : null, null);
        }
        catch (Exception exception) when (exception is InvalidArtifactRenderContract or IOException or UnauthorizedAccessException or UnsafeArtifactPublication)
        {
            return Refused("CLI-RENDER-021", $"The destination's reducer configuration or ownership manifest cannot be read: {exception.Message} Restore the original files for this application and render again; reducer migration is not supported.");
        }
    }

    static (ArtifactRenderInput? Input, ScreenplayDiagnostic? Error) Refused(string code, string message) =>
        (null, new(ScreenplayDiagnosticSeverity.Error, code, message, ReducerConventionInput.RelativePath));
}
