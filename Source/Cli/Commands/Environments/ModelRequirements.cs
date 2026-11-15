// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Run;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// What a Screenplay model needs from an environment: the identity provider, onboarding and system settings keys, and the named secrets. The convention is
/// Screenplay's (<see cref="EnvironmentRequirements"/>); this compiles the model, reads the contracts of the extensions it names from the cache and asks it.
/// </summary>
/// <param name="Application">The compiled model.</param>
/// <param name="Requirements">The keys and secrets the model needs.</param>
/// <param name="Unresolved">The references of extensions whose contract could not be read, so their implicit settings cannot be listed.</param>
public record ModelRequirements(ApplicationSyntax Application, EnvironmentRequirements Requirements, IReadOnlyList<string> Unresolved)
{
    /// <summary>
    /// Compiles the model at a path and derives what it needs.
    /// </summary>
    /// <param name="path">A <c language="text">.play</c> file or a folder of them.</param>
    /// <returns>The requirements.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model does not compile, since what it needs cannot be known.</exception>
    public static ModelRequirements Read(string path)
    {
        var compiler = new PlayFileCompiler();
        var isFolder = Directory.Exists(path);
        var result = isFolder ? compiler.CompileFolder(path).Result : compiler.CompileFile(path).Result;
        if (!result.Success || result.Value is null)
        {
            var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Take(3).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}");
            throw new InvalidOperationException($"The model at '{path}' does not compile, so the keys it needs cannot be listed. {string.Join(' ', errors)}".Trim());
        }

        var application = result.Value;
        var root = Path.GetFullPath(isFolder ? path : Path.GetDirectoryName(path)!);
        var resolution = new ExtensionContractResolver(ExtensionCache.Default, ExtensionResolutionMode.Editor, null, root).Resolve(application, []);
        var contracts = resolution.Contracts.Select(pair => new ExtensionConfigurationContract(
            pair.Key,
            [.. pair.Value.Settings.Select(setting => new ExtensionSettingContract(setting.Name, setting.Exposure == ExtensionSettingExposure.Secret, null, setting.Exposure != ExtensionSettingExposure.Backend))]));
        var unresolved = application.Systems
            .Where(system => system.Extension is not null && !resolution.Contracts.ContainsKey(system.Extension.Reference))
            .Select(system => system.Extension!.Reference)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
        return new(application, EnvironmentRequirements.For(application, contracts), unresolved);
    }

    /// <summary>
    /// Gets a value indicating whether a path holds a Screenplay model.
    /// </summary>
    /// <param name="path">A file or folder path.</param>
    /// <returns><see langword="true"/> for a <c language="text">.play</c> file or a folder holding one.</returns>
    public static bool IsModel(string path) => new ScreenplayModelKeySource().CanRead(path);
}
