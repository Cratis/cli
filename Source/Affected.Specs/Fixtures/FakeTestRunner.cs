// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.Fixtures;

/// <summary>
/// A runner that lists a fixed set of units and applies the real xUnit v3 filter semantics in memory.
/// </summary>
/// <param name="units">The units the project has.</param>
/// <param name="matchWrongly">Whether the listing of a filter also returns a unit that was not selected.</param>
/// <param name="unbuildable">The projects whose build fails.</param>
public class FakeTestRunner(IReadOnlyList<string> units, bool matchWrongly = false, IReadOnlySet<string>? unbuildable = null) : ITestRunner
{
    public List<string> BuiltProjects { get; } = [];

    public List<string> RunProjects { get; } = [];

    public Task<TestAssembly> BuildAsync(string project, CancellationToken cancellationToken)
    {
        if (unbuildable?.Contains(project) == true)
        {
            throw new AffectedException($"Building {project} failed");
        }

        BuiltProjects.Add(project);
        return Task.FromResult(new TestAssembly(project, "/nowhere/" + project + ".dll"));
    }

    public Task<IReadOnlyList<string>> DiscoverUnitsAsync(TestAssembly assembly, UnitGranularity granularity, CancellationToken cancellationToken) =>
        Task.FromResult(units);

    public IReadOnlyList<IReadOnlyList<string>> CreateFilterBatches(IReadOnlyList<SelectionEntry> entries, UnitGranularity granularity, int maxLength) =>
        XunitV3Filter.CreateBatches(entries, granularity, maxLength);

    public IReadOnlyList<string> CreateUnitFilter(string unit, UnitGranularity granularity) => ["-filter", XunitV3Filter.QueriesForUnit(unit, granularity)[0]];

    public Task<IReadOnlyList<string>> ListMatchedAsync(TestAssembly assembly, IReadOnlyList<string> arguments, UnitGranularity granularity, CancellationToken cancellationToken)
    {
        var queries = arguments.Where((_, index) => index % 2 == 1).ToList();
        var matchedUnits = units.Where(unit => queries.Exists(query => Matches(query, unit))).ToList();
        if (matchWrongly)
        {
            matchedUnits.Add("Extra.Not.Selected");
        }

        IReadOnlyList<string> matched = matchedUnits;
        return Task.FromResult(matched);
    }

    public Task<int> RunAsync(TestAssembly assembly, IReadOnlyList<string> arguments, string? resultsFile, CancellationToken cancellationToken)
    {
        RunProjects.Add(assembly.Project);
        return Task.FromResult(0);
    }

    public IReadOnlyList<string> GetCommand(TestAssembly assembly, IReadOnlyList<string> arguments) => ["fake", .. arguments];

    static bool Matches(string query, string unit)
    {
        // /*/namespace/class/* , with a trailing wildcard on the namespace meaning nested namespaces.
        var parts = query.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var ns = parts[1];
        var cls = parts[2];
        var unitNamespace = unit[..unit.LastIndexOf('.')];
        var unitClass = unit[(unit.LastIndexOf('.') + 1)..];
        var namespaceMatches = ns.EndsWith('*') ? unitNamespace.StartsWith(ns[..^1], StringComparison.Ordinal) : unitNamespace == ns;
        return namespaceMatches && (cls == "*" || cls == unitClass);
    }
}
