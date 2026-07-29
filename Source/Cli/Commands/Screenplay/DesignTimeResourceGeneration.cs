// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Makes the MSBuild design-time build a workspace performs generate the strongly typed resource classes that
/// <c>.resx</c> files ask for, so that the loaded compilation holds the same sources a real build compiles.
/// </summary>
/// <remarks>
/// A design-time build only runs the <c>Compile</c> target. <c>PrepareResources</c> — the target chain that turns
/// <c>&lt;Generator&gt;MSBuild:Compile&lt;/Generator&gt;</c> resource entries into <c>.Designer.cs</c> sources and
/// adds them to <c>@(Compile)</c> — is a sibling of <c>Compile</c> within <c>CoreBuild</c> rather than one of its
/// dependencies, so it never runs. Every use of a generated resource class then becomes a compile error, and a
/// perfectly ordinary application looks like it does not compile at all.
/// <para>
/// Injecting a targets file through the <c>CustomAfterMicrosoftCommonTargets</c> hook puts <c>PrepareResources</c>
/// back in front of <c>CoreCompile</c>, which is exactly where a real build runs it. Nothing is hard coded — MSBuild
/// generates the sources into the project's own intermediate output folder and adds them to the compilation itself,
/// whether or not the project has ever been built.
/// </para>
/// </remarks>
public sealed class DesignTimeResourceGeneration : IDisposable
{
    /// <summary>
    /// The MSBuild property naming a targets file that every project imports after the common targets.
    /// </summary>
    public const string HookProperty = "CustomAfterMicrosoftCommonTargets";

    readonly string? _folder;

    DesignTimeResourceGeneration(string? folder, Dictionary<string, string> globalProperties)
    {
        _folder = folder;
        GlobalProperties = globalProperties;
    }

    /// <summary>
    /// Gets the global properties to open the MSBuild workspace with.
    /// </summary>
    public IDictionary<string, string> GlobalProperties { get; }

    /// <summary>
    /// Writes the targets file to a temporary folder and describes how to hand it to a workspace.
    /// </summary>
    /// <returns>The <see cref="DesignTimeResourceGeneration"/> owning the written file.</returns>
    /// <remarks>
    /// Falls back to opening the workspace unchanged when the file cannot be written — a missing resource class is a
    /// far better outcome than not being able to read the application at all.
    /// </remarks>
    public static DesignTimeResourceGeneration Create()
    {
        try
        {
            var folder = Directory.CreateTempSubdirectory("cratis-screenplay-").FullName;
            var file = Path.Combine(folder, "Cratis.Screenplay.DesignTimeResources.targets");
            File.WriteAllText(file, Targets(Environment.GetEnvironmentVariable(HookProperty)));
            return new(folder, new(StringComparer.Ordinal) { [HookProperty] = file });
        }
        catch (IOException)
        {
            return new(null, new(StringComparer.Ordinal));
        }
        catch (UnauthorizedAccessException)
        {
            return new(null, new(StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// Builds the content of the targets file every loaded project imports.
    /// </summary>
    /// <param name="chained">A targets file the hook already pointed at, which the injected one must keep importing; ignored when it does not exist.</param>
    /// <returns>The MSBuild project content.</returns>
    public static string Targets(string? chained)
    {
        List<string> lines = ["<Project>"];

        if (!string.IsNullOrWhiteSpace(chained) && File.Exists(chained))
        {
            lines.Add($"  <Import Project=\"{SecurityElement.Escape(chained)}\" />");
        }

        lines.AddRange(
            "  <!-- Runs the target chain that generates strongly typed resource classes, which a design-time",
            "       build otherwise skips, before the compiler inputs are gathered. -->",
            "  <Target Name=\"CratisScreenplayPrepareResources\"",
            "          BeforeTargets=\"CoreCompile\"",
            "          DependsOnTargets=\"PrepareResources\" />",
            "</Project>",
            string.Empty);

        return string.Join('\n', lines);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_folder is null || !Directory.Exists(_folder))
        {
            return;
        }

        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
