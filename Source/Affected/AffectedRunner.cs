// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Carries out a plan: runs each test project as decided.
/// </summary>
/// <param name="runner">The test runner strategy.</param>
public class AffectedRunner(ITestRunner runner)
{
    /// <summary>
    /// Runs the planned tests.
    /// </summary>
    /// <param name="report">The plan.</param>
    /// <param name="resultsDirectory">Where TRX result files are written.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>Zero when every run passed, otherwise the first failing exit code. A project that does not build fails the run but never stops the others.</returns>
    public async Task<int> RunAsync(SelectionReport report, string resultsDirectory, CancellationToken cancellationToken = default)
    {
        var exitCode = 0;
        foreach (var plan in report.Projects.Where(plan => plan.Selection.Mode != SelectionMode.Skip))
        {
            TestAssembly assembly;
            try
            {
                assembly = plan.Assembly ?? await runner.BuildAsync(plan.Selection.Project, cancellationToken);
            }
            catch (AffectedException exception)
            {
                // One project that does not build must not hide the results of every project after it.
                await Console.Error.WriteLineAsync($"::error title=affected-tests::{plan.Selection.Project} could not be built or located: {exception.Message}");
                exitCode = exitCode == 0 ? 1 : exitCode;
                continue;
            }

            var name = plan.Selection.Project.Replace('/', '_');
            var batches = BatchesOf(plan);

            for (var index = 0; index < batches.Count; index++)
            {
                var results = Path.Combine(resultsDirectory, $"{name}.{index}.trx");
                var code = await runner.RunAsync(assembly, batches[index], results, cancellationToken);
                if (code != 0 && exitCode == 0)
                {
                    exitCode = code;
                }
            }
        }

        return exitCode;
    }

    static IReadOnlyList<IReadOnlyList<string>> BatchesOf(ProjectPlan plan)
    {
        if (plan.Selection.Mode == SelectionMode.All)
        {
            return [[]];
        }

        return plan.FilterBatches;
    }
}
