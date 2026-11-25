// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// How a kind of test project is built, listed, filtered and run. A strategy exists per runner technology.
/// </summary>
public interface ITestRunner
{
    /// <summary>
    /// Builds a test project.
    /// </summary>
    /// <param name="project">The project file path, repository relative.</param>
    /// <param name="cancellationToken">Cancels the build.</param>
    /// <returns>The built assembly.</returns>
    Task<TestAssembly> BuildAsync(string project, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the units a built project currently has.
    /// </summary>
    /// <param name="assembly">The built assembly.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="cancellationToken">Cancels the listing.</param>
    /// <returns>The unit names.</returns>
    Task<IReadOnlyList<string>> DiscoverUnitsAsync(TestAssembly assembly, UnitGranularity granularity, CancellationToken cancellationToken);

    /// <summary>
    /// Expresses a selection as runner arguments, split into several runs when it is too long for one.
    /// </summary>
    /// <param name="entries">The selection.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="maxLength">The longest argument text of one run, in characters.</param>
    /// <returns>One argument list per run.</returns>
    IReadOnlyList<IReadOnlyList<string>> CreateFilterBatches(IReadOnlyList<SelectionEntry> entries, UnitGranularity granularity, int maxLength);

    /// <summary>
    /// Expresses a single unit as runner arguments.
    /// </summary>
    /// <param name="unit">The unit name.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <returns>The arguments.</returns>
    IReadOnlyList<string> CreateUnitFilter(string unit, UnitGranularity granularity);

    /// <summary>
    /// Asks the runner which units a filter matches, without running them.
    /// </summary>
    /// <param name="assembly">The built assembly.</param>
    /// <param name="arguments">The filter arguments.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="cancellationToken">Cancels the listing.</param>
    /// <returns>The matched unit names.</returns>
    Task<IReadOnlyList<string>> ListMatchedAsync(TestAssembly assembly, IReadOnlyList<string> arguments, UnitGranularity granularity, CancellationToken cancellationToken);

    /// <summary>
    /// Runs tests.
    /// </summary>
    /// <param name="assembly">The built assembly.</param>
    /// <param name="arguments">The filter arguments; empty runs everything.</param>
    /// <param name="resultsFile">Where to write a TRX result file, or null for none.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The exit code of the run.</returns>
    Task<int> RunAsync(TestAssembly assembly, IReadOnlyList<string> arguments, string? resultsFile, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the command that runs tests, for wrapping in a coverage collector.
    /// </summary>
    /// <param name="assembly">The built assembly.</param>
    /// <param name="arguments">The filter arguments.</param>
    /// <returns>The executable followed by its arguments.</returns>
    IReadOnlyList<string> GetCommand(TestAssembly assembly, IReadOnlyList<string> arguments);
}
