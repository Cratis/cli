// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// One project file and what the affected-test computation needs to know about it.
/// </summary>
/// <param name="Path">The repository relative path of the project file.</param>
/// <param name="References">The repository relative paths of the project files it references.</param>
/// <param name="AnalyzerReferences">The subset of <paramref name="References"/> consumed as analyzers or source generators.</param>
/// <param name="IsTestProject">Whether it runs tests.</param>
/// <param name="IsGenerator">Whether it is itself a source generator or analyzer.</param>
/// <param name="ExternalInputs">Files and folders outside its directory that feed it; a folder ends with a slash.</param>
public record ProjectDefinition(
    string Path,
    IReadOnlyList<string> References,
    IReadOnlyList<string> AnalyzerReferences,
    bool IsTestProject,
    bool IsGenerator,
    IReadOnlyList<string> ExternalInputs)
{
    /// <summary>
    /// Gets the repository relative directory of the project, empty for the repository root.
    /// </summary>
    public string Directory => PathNormalizer.DirectoryOf(Path);

    /// <summary>
    /// Gets whether a file lives in the directory of this project.
    /// </summary>
    /// <param name="file">The repository relative path.</param>
    /// <returns>True when the file is inside the project directory.</returns>
    public bool Contains(string file) =>
        Directory.Length == 0 || file.StartsWith(Directory + "/", StringComparison.Ordinal);
}
