// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Tells whether the projects a Screenplay is generated from have been restored.
/// </summary>
/// <remarks>
/// An unrestored project still opens and still yields a compilation — one with no package reference resolved, in
/// which every framework type the application uses reads as missing. The generator then reports a page of artifacts
/// it cannot recognize and writes a document describing nobody's application, and none of it says the one thing that
/// is actually wrong. Asking for the assets file up front turns the consequences back into the cause.
/// </remarks>
public static class ProjectRestoreState
{
    /// <summary>
    /// The file a restore writes into the intermediate output folder of a project.
    /// </summary>
    public const string AssetsFileName = "project.assets.json";

    const string DefaultIntermediateFolderName = "obj";
    const int LevelsAboveIntermediateAssembly = 4;

    /// <summary>
    /// Determines whether the given project has been restored.
    /// </summary>
    /// <param name="projectFilePath">The full path of the project file.</param>
    /// <param name="intermediateAssemblyPath">The full path of the assembly the project compiles into its intermediate output folder.</param>
    /// <returns><see langword="true"/> when the project has been restored, and when it cannot be told.</returns>
    /// <remarks>
    /// A project neither path says anything about is taken for restored — this exists to explain a failure and must
    /// never invent one.
    /// </remarks>
    public static bool IsRestored(string? projectFilePath, string? intermediateAssemblyPath)
    {
        var folders = FoldersHoldingTheAssetsFile(projectFilePath, intermediateAssemblyPath);
        return folders.Count == 0 || folders.Exists(folder => File.Exists(Path.Combine(folder, AssetsFileName)));
    }

    /// <summary>
    /// Builds the message reported for projects that have not been restored.
    /// </summary>
    /// <param name="projectNames">The names of the projects that have not been restored, at least one, ordered.</param>
    /// <returns>The message.</returns>
    public static string MessageFor(IReadOnlyList<string> projectNames) =>
        $"{Describe(projectNames)} not been restored, so every type the application references reads as missing — run 'dotnet restore' and generate again";

    /// <summary>
    /// Gets the folders the assets file of a project could sit in.
    /// </summary>
    /// <param name="projectFilePath">The full path of the project file.</param>
    /// <param name="intermediateAssemblyPath">The full path of the assembly the project compiles into its intermediate output folder.</param>
    /// <returns>The folders to look in, empty when neither path says anything.</returns>
    /// <remarks>
    /// The assets file sits at the root of the intermediate output folder, which is <c>obj</c> beside the project
    /// until something moves it — the artifacts output layout of the SDK moves it out of the project folder
    /// altogether. Where it went is only knowable from the assembly the project compiles into it, and the assets
    /// file sits above the configuration and target framework folders that assembly is in, so every folder on the
    /// way up from it is a place the assets file could be.
    /// </remarks>
    static List<string> FoldersHoldingTheAssetsFile(string? projectFilePath, string? intermediateAssemblyPath)
    {
        var folders = new List<string>();

        if (FolderOf(projectFilePath) is { } project)
        {
            folders.Add(Path.Combine(project, DefaultIntermediateFolderName));
        }

        var current = FolderOf(intermediateAssemblyPath);
        for (var level = 0; current is not null && level < LevelsAboveIntermediateAssembly; level++)
        {
            folders.Add(current);
            current = Path.GetDirectoryName(current);
        }

        return folders;
    }

    static string? FolderOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var folder = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(folder) ? null : folder;
    }

    static string Describe(IReadOnlyList<string> projectNames) =>
        projectNames.Count == 1
            ? $"'{projectNames[0]}' has"
            : $"'{projectNames[0]}' and {projectNames.Count - 1} more have";
}
