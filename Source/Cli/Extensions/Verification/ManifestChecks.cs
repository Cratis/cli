// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The manifest checks the compiler does not make: stated requirements, and the format and size of the icon and the marketplace images.
/// </summary>
public static class ManifestChecks
{
    /// <summary>
    /// Runs the checks.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="root">The package folder.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<VerifyDiagnostic> Run(ExtensionContract contract, string root)
    {
        const string check = ExtensionVerifyCodes.ManifestCheck;
        if (contract.Requirements.IsDefaultOrEmpty)
        {
            yield return new(check, ExtensionVerifyCodes.NoRequirements, VerifySeverity.Error, "The contract states no requirement. Add 'requires stage \"<range>\"' and 'requires platform <name>' so a host knows when it can use the package.", ExtensionVerifier.ContractFile, contract.Location.Line);
        }

        foreach (var finding in Image(root, contract.Icon, ExtensionVerifyCodes.IconLimit, "icon")) yield return finding;
        foreach (var image in contract.Images)
        {
            foreach (var finding in Image(root, image.Path, ExtensionVerifyCodes.ImageLimit, "image")) yield return finding;
            if (image.Caption is null)
            {
                yield return new(check, ExtensionVerifyCodes.MissingImageCaption, VerifySeverity.Warning, $"The image '{image.Path}' declares no caption. Add 'caption \"<text>\"' below it so the marketplace can describe it.", ExtensionVerifier.ContractFile, image.Location.Line);
            }
            else if (string.IsNullOrWhiteSpace(image.Caption))
            {
                yield return new(check, ExtensionVerifyCodes.BlankImageCaption, VerifySeverity.Error, $"The caption of the image '{image.Path}' is blank.", ExtensionVerifier.ContractFile, image.Location.Line);
            }
        }
    }

    static IEnumerable<VerifyDiagnostic> Image(string root, string? path, long limit, string kind)
    {
        const string check = ExtensionVerifyCodes.ManifestCheck;
        if (path is null) yield break;
        var full = Path.GetFullPath(Path.Combine(root, path));
        if (!full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal) || !File.Exists(full)) yield break;

        var content = File.ReadAllBytes(full);
        var format = ExtensionImageFormat.Of(content);
        if (format is null || format != ExtensionImageFormat.Claimed(path))
        {
            var actual = format is null ? "none of them" : $"{format}, not {ExtensionImageFormat.Claimed(path)}";
            yield return new(check, ExtensionVerifyCodes.UnsupportedImageFormat, VerifySeverity.Error, $"The {kind} '{path}' must be one of {string.Join(", ", ExtensionImageFormat.Supported)} and its content must match its name; it is {actual}.", path);
        }

        if (content.Length > limit)
        {
            yield return new(check, ExtensionVerifyCodes.ImageTooLarge, VerifySeverity.Error, $"The {kind} '{path}' is {content.Length / 1024} KB; the limit is {limit / 1024} KB.", path);
        }
    }
}
