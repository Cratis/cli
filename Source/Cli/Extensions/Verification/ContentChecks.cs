// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The checks that the package ships what its contract declares and nothing else, and that its placeholders are within their limit.
/// </summary>
public static class ContentChecks
{
    /// <summary>
    /// Checks the contents.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="root">The package folder.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<VerifyDiagnostic> Contents(ExtensionContract contract, string root)
    {
        const string check = ExtensionVerifyCodes.ContentsCheck;
        if (contract.Frontend is null)
        {
            foreach (var component in contract.Components.Where(component => ExtensionVerifier.IsCodeKind(component.Kind)))
            {
                yield return new(check, ExtensionVerifyCodes.ComponentWithoutFrontend, VerifySeverity.Error, $"The component '{component.Name}' is {component.Kind} but the contract names no frontend. Add 'frontend \"<path>\"'.", ExtensionVerifier.ContractFile, component.Location.Line);
            }
        }

        if (contract.Backend is { } backend && !Exists(root, backend.Assembly))
        {
            yield return new(check, ExtensionVerifyCodes.BackendNotShipped, VerifySeverity.Error, $"The backend '{backend.Assembly}' is not in the package.", backend.Assembly, backend.Location.Line);
        }

        var declared = Declared(contract);
        var directories = new[] { contract.Frontend, contract.Backend?.Assembly }
            .Where(path => path is not null).Select(path => Path.GetDirectoryName(Normalize(path!))!.Replace('\\', '/')).Where(directory => directory.Length > 0).ToArray();
        var ignore = ExtensionIgnoreFile.Read(root);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(root, file).Replace('\\', '/')).Order(StringComparer.Ordinal))
        {
            if (declared.Contains(file) || ignore.Ignores(file) || directories.Any(directory => file.StartsWith(directory + "/", StringComparison.Ordinal))) continue;
            yield return new(check, ExtensionVerifyCodes.UndeclaredFile, VerifySeverity.Error, $"'{file}' is shipped but the contract does not declare it. Declare it, or list it in {ExtensionIgnoreFile.FileName} if the build does not ship it.", file);
        }
    }

    /// <summary>
    /// Checks the size of every placeholder.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="root">The package folder.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<VerifyDiagnostic> Placeholders(ExtensionContract contract, string root)
    {
        foreach (var path in contract.Components.Select(component => component.Placeholder).Where(path => path is not null))
        {
            var full = Path.GetFullPath(Path.Combine(root, path!));
            if (!full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal) || !File.Exists(full)) continue;
            var size = new FileInfo(full).Length;
            if (size > ExtensionVerifyCodes.PlaceholderLimit)
            {
                yield return new(ExtensionVerifyCodes.PlaceholdersCheck, ExtensionVerifyCodes.PlaceholderTooLarge, VerifySeverity.Error, $"The placeholder '{path}' is {size / 1024} KB; the limit is {ExtensionVerifyCodes.PlaceholderLimit / 1024} KB.", path);
            }
        }
    }

    static bool Exists(string root, string path)
    {
        var full = Path.GetFullPath(Path.Combine(root, path));
        return full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal) && File.Exists(full);
    }

    static string Normalize(string path) => path.Replace('\\', '/').TrimStart('.', '/');

    static HashSet<string> Declared(ExtensionContract contract)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal) { ExtensionVerifier.ContractFile };
        foreach (var path in new[] { contract.Icon, contract.Frontend, contract.Backend?.Assembly }.Concat(contract.Images.Select(image => image.Path)).Concat(contract.Components.Select(component => component.Placeholder)))
        {
            if (path is not null) declared.Add(Normalize(path));
        }

        return declared;
    }
}
