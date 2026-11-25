// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The projects of a repository at one revision and the references between them.
/// </summary>
public class ProjectGraph
{
    static readonly string[] _projectExtensions = [".csproj", ".fsproj", ".vbproj"];

    readonly Dictionary<string, HashSet<string>> _dependents;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectGraph"/> class.
    /// </summary>
    /// <param name="projects">The projects of the repository.</param>
    public ProjectGraph(IEnumerable<ProjectDefinition> projects)
    {
        var all = projects.ToList();
        var paths = all.Select(project => project.Path).ToHashSet(StringComparer.Ordinal);
        var generators = all.SelectMany(project => project.AnalyzerReferences).ToHashSet(StringComparer.Ordinal);

        Projects = all
            .Select(project => project with
            {
                References = ResolveReferences(project.References, paths),
                IsGenerator = project.IsGenerator || generators.Contains(project.Path)
            })
            .ToDictionary(project => project.Path, StringComparer.Ordinal);

        _dependents = Projects.Values
            .SelectMany(project => project.References.Select(reference => (Reference: reference, project.Path)))
            .GroupBy(pair => pair.Reference, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Path).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the projects by repository relative path.
    /// </summary>
    public IReadOnlyDictionary<string, ProjectDefinition> Projects { get; }

    /// <summary>
    /// Reads the project graph of a repository at a revision.
    /// </summary>
    /// <param name="git">The repository.</param>
    /// <param name="revision">The revision to read.</param>
    /// <param name="configuration">The repository configuration.</param>
    /// <returns>The graph.</returns>
    /// <exception cref="AffectedException">A project file cannot be read or parsed.</exception>
    public static ProjectGraph Load(IGitRepository git, string revision, AffectedConfiguration configuration)
    {
        var definitions = new List<ProjectDefinition>();
        foreach (var path in git.ListFiles(revision).Where(file => _projectExtensions.Any(extension => file.EndsWith(extension, StringComparison.Ordinal))))
        {
            var xml = git.ReadFile(revision, path) ?? throw new AffectedException($"{path} is listed at {revision} but cannot be read.");
            definitions.Add(ProjectParser.Parse(path, xml, configuration));
        }

        return new ProjectGraph(definitions);
    }

    /// <summary>
    /// Finds every project that owns a file: the deepest project whose directory holds it, and every project that pulls it in from outside its directory.
    /// </summary>
    /// <param name="file">The repository relative path.</param>
    /// <returns>The paths of the owning project files, empty when the file belongs to no project.</returns>
    public IReadOnlyList<string> OwnersOf(string file)
    {
        var owners = new List<string>();
        var deepest = Projects.Values
            .Where(project => project.Contains(file))
            .OrderByDescending(project => project.Directory.Length)
            .ThenBy(project => project.Path, StringComparer.Ordinal)
            .FirstOrDefault();
        if (deepest is not null)
        {
            owners.Add(deepest.Path);
        }

        owners.AddRange(Projects.Values
            .Where(project => project.ExternalInputs.Any(input => input.EndsWith('/') ? file.StartsWith(input, StringComparison.Ordinal) : input == file))
            .Select(project => project.Path));
        return [.. owners.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Finds every project that depends, directly or through others, on any of the given projects.
    /// </summary>
    /// <param name="projects">The project file paths.</param>
    /// <returns>The paths of the dependents, not including the given projects unless one depends on another.</returns>
    public IReadOnlySet<string> DependentsOf(IEnumerable<string> projects)
    {
        HashSet<string> found = [];
        var queue = new Queue<string>(projects);
        while (queue.Count > 0)
        {
            if (!_dependents.TryGetValue(queue.Dequeue(), out var direct))
            {
                continue;
            }

            foreach (var dependent in direct.Where(found.Add))
            {
                queue.Enqueue(dependent);
            }
        }

        return found;
    }

    static string[] ResolveReferences(IReadOnlyList<string> references, HashSet<string> paths)
    {
        var resolved = new List<string>();
        foreach (var reference in references)
        {
            if (reference.Contains('*', StringComparison.Ordinal))
            {
                resolved.AddRange(paths.Where(path => Glob.IsMatch(reference, path)));
            }
            else if (paths.Contains(reference))
            {
                resolved.Add(reference);
            }
        }

        return [.. resolved.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }
}
