// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Environments;

/// <summary>
/// The environments of one project: the committed files in <c language="csharp">.cratis/environments</c> and the per-user selection in <c language="csharp">.cratis/local</c>.
/// </summary>
public class ProjectEnvironments
{
    /// <summary>
    /// The environment variable that overrides the selection file.
    /// </summary>
    public const string EnvironmentVariable = "CRATIS_ENVIRONMENT";

    /// <summary>
    /// The ignore entry that keeps the per-user selection out of version control.
    /// </summary>
    public const string LocalIgnoreEntry = ".cratis/local/";

    ProjectEnvironments(string projectRoot) => ProjectRoot = projectRoot;

    /// <summary>
    /// Gets the project root: the folder that holds <c language="csharp">.cratis</c>.
    /// </summary>
    public string ProjectRoot { get; }

    /// <summary>
    /// Gets the folder holding the environment files.
    /// </summary>
    public string EnvironmentsDirectory => Path.Combine(ProjectRoot, ".cratis", "environments");

    /// <summary>
    /// Gets the path of the per-user selection file.
    /// </summary>
    public string SelectionFile => Path.Combine(ProjectRoot, ".cratis", "local", "selected-environment");

    /// <summary>
    /// Creates the environments of a known project root.
    /// </summary>
    /// <param name="projectRoot">The project root.</param>
    /// <returns>The environments.</returns>
    public static ProjectEnvironments ForProject(string projectRoot) => new(Path.GetFullPath(projectRoot));

    /// <summary>
    /// Finds the project root by walking up from a folder to the nearest <c language="csharp">.cratis</c> folder (ignoring the one in the user profile),
    /// then to the nearest Git root, and otherwise using the folder itself.
    /// </summary>
    /// <param name="start">The folder to start from.</param>
    /// <returns>The environments of the project found.</returns>
    public static ProjectEnvironments Discover(string start)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var full = Path.GetFullPath(start);
        for (var directory = new DirectoryInfo(full); directory is not null; directory = directory.Parent)
        {
            var isHome = string.Equals(directory.FullName.TrimEnd(Path.DirectorySeparatorChar), home.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal);
            if (!isHome && Directory.Exists(Path.Combine(directory.FullName, ".cratis")))
            {
                return new(directory.FullName);
            }
        }

        for (var directory = new DirectoryInfo(full); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return new(directory.FullName);
            }
        }

        return new(full);
    }

    /// <summary>
    /// Gets the path of an environment's file.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>The path.</returns>
    public string PathFor(string name) => Path.Combine(EnvironmentsDirectory, $"{EnvironmentName.Validate(name)}.yaml");

    /// <summary>
    /// Determines whether an environment exists.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>True when its file exists.</returns>
    public bool Exists(string name) => File.Exists(PathFor(name));

    /// <summary>
    /// Lists the environment names, sorted.
    /// </summary>
    /// <returns>The names.</returns>
    public IReadOnlyList<string> List() =>
        Directory.Exists(EnvironmentsDirectory)
            ? [.. Directory.EnumerateFiles(EnvironmentsDirectory, "*.yaml").Select(Path.GetFileNameWithoutExtension).OfType<string>().Where(EnvironmentName.IsValid).Order(StringComparer.Ordinal)]
            : [];

    /// <summary>
    /// Loads the document of an environment.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>The document.</returns>
    /// <exception cref="EnvironmentNotFoundException">Thrown when the environment does not exist.</exception>
    public EnvironmentDocument Load(string name)
    {
        var path = PathFor(name);
        return File.Exists(path) ? EnvironmentYaml.Parse(File.ReadAllText(path)) : throw new EnvironmentNotFoundException(name);
    }

    /// <summary>
    /// Saves the document of an environment, replacing the file atomically.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <param name="document">The document.</param>
    public void Save(string name, EnvironmentDocument document)
    {
        var path = PathFor(name);
        Directory.CreateDirectory(EnvironmentsDirectory);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, EnvironmentYaml.Write(document), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>
    /// Deletes an environment file. A selection of that environment is cleared.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>True when a file was deleted.</returns>
    public bool Delete(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        if (string.Equals(ReadSelection(), name, StringComparison.Ordinal))
        {
            File.Delete(SelectionFile);
        }

        return true;
    }

    /// <summary>
    /// Records the per-user selection and makes sure the ignore file keeps it out of version control.
    /// </summary>
    /// <param name="name">The environment name.</param>
    public void Select(string name)
    {
        EnvironmentName.Validate(name);
        Directory.CreateDirectory(Path.GetDirectoryName(SelectionFile)!);
        File.WriteAllText(SelectionFile, name + Environment.NewLine, new UTF8Encoding(false));
        EnsureLocalIgnored();
    }

    /// <summary>
    /// Resolves the effective environment. Precedence: the explicit option, <c language="csharp">CRATIS_ENVIRONMENT</c>, the selection file, then <c language="csharp">development</c>.
    /// </summary>
    /// <param name="explicitName">The value of <c language="csharp">--environment</c> / <c language="csharp">-e</c>, if any.</param>
    /// <returns>The environment and where it came from.</returns>
    public SelectedEnvironment Resolve(string? explicitName)
    {
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return new(EnvironmentName.Validate(explicitName.Trim()), SelectionSource.Option);
        }

        var variable = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(variable))
        {
            return new(EnvironmentName.Validate(variable.Trim()), SelectionSource.EnvironmentVariable);
        }

        var selected = ReadSelection();
        return selected is not null && EnvironmentName.IsValid(selected)
            ? new(selected, SelectionSource.SelectionFile)
            : new(EnvironmentName.Default, SelectionSource.Default);
    }

    /// <summary>
    /// Adds <see cref="LocalIgnoreEntry"/> to the project's <c language="csharp">.gitignore</c> when it is missing.
    /// </summary>
    /// <returns>True when the file was changed.</returns>
    public bool EnsureLocalIgnored()
    {
        var path = Path.Combine(ProjectRoot, ".gitignore");
        var existing = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        var covered = existing.Split('\n').Select(line => line.Trim().TrimEnd('/')).Any(line => string.Equals(line, ".cratis/local", StringComparison.Ordinal) || string.Equals(line, "/.cratis/local", StringComparison.Ordinal) || string.Equals(line, ".cratis/local/*", StringComparison.Ordinal));
        if (covered)
        {
            return false;
        }

        var prefix = existing.Length == 0 || existing.EndsWith('\n') ? string.Empty : Environment.NewLine;
        File.AppendAllText(path, $"{prefix}{LocalIgnoreEntry}{Environment.NewLine}");
        return true;
    }

    string? ReadSelection()
    {
        if (!File.Exists(SelectionFile))
        {
            return null;
        }

        var text = File.ReadAllText(SelectionFile).Trim();
        return text.Length > 0 ? text : null;
    }
}
