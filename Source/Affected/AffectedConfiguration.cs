// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Affected;

/// <summary>
/// Per-repository settings, read from .cratis/affected.json. Every member has a safe default.
/// </summary>
public class AffectedConfiguration
{
    /// <summary>
    /// The repository relative path of the configuration file. It is itself a run-everything trigger.
    /// </summary>
    public const string FilePath = ".cratis/affected.json";

    /// <summary>
    /// The repository relative folder that holds the generated coverage maps. Changes there never select tests.
    /// </summary>
    public const string MapsFolder = ".cratis/affected/maps";

    /// <summary>
    /// The files whose change can alter every build and so runs everything unless the repository overrides the list.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultRunEverythingTriggers =
    [
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Build.rsp",
        "Directory.Packages.props",
        "global.json",
        "NuGet.config",
        ".editorconfig",
        ".globalconfig",
        FilePath
    ];

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Gets the globs of files that force a full run. Replaces the defaults when set.
    /// </summary>
    public IReadOnlyList<string> RunEverythingTriggers { get; init; } = DefaultRunEverythingTriggers;

    /// <summary>
    /// Gets the globs of files outside every project that are explicitly handled by some other job, so they do not force a full run.
    /// </summary>
    public IReadOnlyList<string> MappedPaths { get; init; } = [];

    /// <summary>
    /// Gets globs of project files that are test projects although they declare no test SDK, for conventions set in shared props.
    /// </summary>
    public IReadOnlyList<string> TestProjects { get; init; } = [];

    /// <summary>
    /// Gets globs of project files that are not test projects although they reference a test framework, such as fixtures and samples.
    /// </summary>
    public IReadOnlyList<string> NonTestProjects { get; init; } = [];

    /// <summary>
    /// Gets globs of project files that are source generators or analyzers although they do not say so themselves.
    /// </summary>
    public IReadOnlyList<string> GeneratorProjects { get; init; } = [];

    /// <summary>
    /// Gets project references the project files do not declare, as test project globs mapped to the project files they need built first.
    /// For a spec that starts the server it spec'd from its build output, the way a project reference would.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ImplicitReferences { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Gets what is selected for a file the coverage map does not know.
    /// </summary>
    public AddedFilePolicy AddedFilePolicy { get; init; } = AddedFilePolicy.Project;

    /// <summary>
    /// Gets what is selected for a changed or removed file that no test of the project has covered. Defaults to the project,
    /// the safe choice: a file nothing executed can still be read by reflection or type discovery.
    /// </summary>
    public AddedFilePolicy UnmappedFilePolicy { get; init; } = AddedFilePolicy.Project;

    /// <summary>
    /// Gets the selected fraction of a namespace at which it is rolled up into one entry.
    /// </summary>
    public double RollupThreshold { get; init; } = 0.5;

    /// <summary>
    /// Gets the number of units a namespace needs before it can be rolled up.
    /// </summary>
    public int RollupMinUnits { get; init; } = 5;

    /// <summary>
    /// Gets the fraction, from 0 to 1, of unselected units that run anyway as a canary.
    /// </summary>
    public double SampleRate { get; init; }

    /// <summary>
    /// Gets whether a unit is a test class or an individual test.
    /// </summary>
    public UnitGranularity Granularity { get; init; } = UnitGranularity.Class;

    /// <summary>
    /// Gets extra arguments for the dotnet build of every test project, for example properties that turn analyzers off
    /// when another job already runs them.
    /// </summary>
    public IReadOnlyList<string> BuildArguments { get; init; } = [];

    /// <summary>
    /// Gets the most coverage runs one test project is split into when units are grouped, or 0 for no limit. Neighbouring
    /// namespaces share a run beyond it, so a project with hundreds of namespaces builds its map in bounded time. Selection
    /// stays safe, only wider.
    /// </summary>
    public int MaxAttributionGroups { get; init; }

    /// <summary>
    /// Gets how many units share one coverage run when a map is built.
    /// </summary>
    public AttributionGroup AttributionGroup { get; init; } = AttributionGroup.Unit;

    /// <summary>
    /// Gets whether each new map is unioned with the previous one, to damp non-deterministic coverage.
    /// </summary>
    public bool UnionPreviousMap { get; init; }

    /// <summary>
    /// Gets the longest filter text, in characters, passed in one test run before the selection is split into batches.
    /// </summary>
    public int MaxFilterLength { get; init; } = 100_000;

    /// <summary>
    /// Reads the configuration of a repository.
    /// </summary>
    /// <param name="repositoryRoot">The working tree root.</param>
    /// <returns>The configuration, or the defaults when the file does not exist.</returns>
    /// <exception cref="AffectedException">The file is not valid JSON.</exception>
    public static AffectedConfiguration Load(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, FilePath);
        if (!File.Exists(path))
        {
            return new AffectedConfiguration();
        }

        try
        {
            return JsonSerializer.Deserialize<AffectedConfiguration>(File.ReadAllText(path), _json) ?? new AffectedConfiguration();
        }
        catch (JsonException exception)
        {
            throw new AffectedException($"{FilePath} is not valid: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Gets the project files a project needs built and tracked as references without declaring them.
    /// </summary>
    /// <param name="project">The repository relative path of the project.</param>
    /// <returns>The paths of the project files.</returns>
    public IReadOnlyList<string> ImplicitReferencesOf(string project) =>
        [.. ImplicitReferences.Where(pair => Glob.IsMatch(pair.Key, project)).SelectMany(pair => pair.Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Gets whether a changed file forces a full run.
    /// </summary>
    /// <param name="path">The repository relative path.</param>
    /// <returns>True for a trigger.</returns>
    public bool IsRunEverythingTrigger(string path) => Glob.IsMatchAny(RunEverythingTriggers, path, ignoreCase: true);

    /// <summary>
    /// Gets whether a changed file never selects anything, because the tool wrote it or another job owns it.
    /// </summary>
    /// <param name="path">The repository relative path.</param>
    /// <returns>True when the file is ignored.</returns>
    public bool IsIgnored(string path) =>
        path.StartsWith(".cratis/affected/", StringComparison.Ordinal) || Glob.IsMatchAny(MappedPaths, path);
}
