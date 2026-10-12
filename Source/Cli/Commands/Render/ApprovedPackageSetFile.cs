// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Reads the approved package set document that <c language="shell">cratis render</c> and <c language="shell">cratis run</c>
/// both accept with <c language="shell">--package-set</c>.
/// </summary>
internal static class ApprovedPackageSetFile
{
    /// <summary>
    /// The hint shown with a package set that cannot be used.
    /// </summary>
    public const string Hint = "Provide an approved package set document: { \"version\": 1, \"packages\": [ { \"manifest\": { ... }, \"module\": { ... } } ] }";

    /// <summary>
    /// Reads and validates the approved package set document.
    /// </summary>
    /// <param name="path">The full path of the document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The set, or the reason it cannot be used.</returns>
    public static async Task<(ApprovedPackageSet? Set, string? Error, bool NotFound)> Read(string path, CancellationToken cancellationToken)
    {
        string text;
        try
        {
            text = await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return (null, "The approved package set file was not found", true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, $"The approved package set file cannot be read: {exception.Message}", false);
        }

        var read = ApprovedPackageSetJson.Read(text, CratisRendering.ShippedPackageNames);
        return read.Succeeded
            ? (read.Set, null, false)
            : (null, $"The approved package set is not valid: {string.Join(' ', read.Diagnostics)}", false);
    }
}
