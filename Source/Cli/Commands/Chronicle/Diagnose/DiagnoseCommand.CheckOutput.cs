// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    static bool CheckCompleted(DiagnoseData data, string check) => !data.ChecksCouldNotRun.Any(x => x.Check == check);

    static string CheckDetail(DiagnoseData data, string check, string completedDetail, bool reasonsAbove = false) => CheckCompleted(data, check)
        ? completedDetail
        : $"[{OutputFormatter.Danger.ToMarkup()}]could not check (see reasons {(reasonsAbove ? "above" : "below")})[/]";

    static bool ShowScopeTails(DiagnoseData data) => data.IsAggregate && data.Scopes.Count > 0;

    static string ScopeTailDetail(DiagnoseData scope) => CheckCompleted(scope, "Event sequence")
        ? scope.EventSequenceTail?.ToString() ?? "empty"
        : $"[{OutputFormatter.Danger.ToMarkup()}]could not check[/]";

    static string DescribeCheckFailure(DiagnoseCheckFailure check) =>
        $"{check.EventStore ?? "server"}/{check.Namespace ?? "all"}: {check.Check}: {check.Reason}";

    static IReadOnlyList<object>? ObserverRequirementsJson(DiagnoseData data) => data.ObserverRequirements.Count == 0 ? null :
        [.. data.ObserverRequirements.Select(requirement => (object)new
        {
            observerId = requirement.ObserverId,
            eventStore = requirement.EventStore,
            @namespace = requirement.Namespace,
            status = requirement.Status.ToString().ToLowerInvariant(),
            observedState = requirement.ObservedState,
            reason = requirement.Reason
        })];

    static string DescribeRequirement(DiagnoseObserverRequirement requirement) =>
        $"{requirement.EventStore}/{requirement.Namespace}/{requirement.ObserverId}: {requirement.Status}; observed state: {requirement.ObservedState ?? "none"}; {requirement.Reason}";

    static void RenderObserverRequirements(DiagnoseData data)
    {
        if (data.ObserverRequirements.Count == 0) return;
        var satisfied = data.ObserverRequirements.Count(requirement => requirement.Status == DiagnoseObserverRequirementStatus.Satisfied);
        WriteCheck(data.ObserverRequirementsSatisfied, "Required observers", $"{satisfied}/{data.ObserverRequirements.Count} satisfied");
        foreach (var requirement in data.ObserverRequirements)
        {
            AnsiConsole.MarkupLine($"  {DescribeRequirement(requirement).EscapeMarkup()}");
        }

        AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]state only; not proof of event freshness or delivery[/]");
    }

    static void RenderPlainObserverRequirements(DiagnoseData data)
    {
        if (data.ObserverRequirements.Count == 0) return;
        Console.WriteLine($"observer_requirements_satisfied={data.ObserverRequirementsSatisfied.ToString().ToLowerInvariant()}");
        foreach (var requirement in data.ObserverRequirements)
        {
            Console.WriteLine($"observer_requirement={PlainValue(requirement.ObserverId)} event_store={PlainValue(requirement.EventStore)} namespace={PlainValue(requirement.Namespace)} status={PlainValue(requirement.Status.ToString().ToLowerInvariant())} state={PlainValue(requirement.ObservedState)} reason={PlainValue(requirement.Reason)}");
        }
    }

    static void AddPendingObserverRequirements(Table table, DiagnoseData data)
    {
        if (data.ObserverRequirements.Count == 0) return;
        table.AddRow($"[{OutputFormatter.Muted.ToMarkup()}]·[/]", $"[{OutputFormatter.Accent.ToMarkup()}]Required observers[/]", "checking…");
    }

    static void AddObserverRequirements(Table table, DiagnoseData data)
    {
        const int maximumRows = 3;
        foreach (var requirement in data.ObserverRequirements.Take(maximumRows))
        {
            var satisfied = requirement.Status == DiagnoseObserverRequirementStatus.Satisfied;
            var color = satisfied ? OutputFormatter.Success : OutputFormatter.Danger;
            var icon = satisfied ? "✓" : "✗";
            table.AddRow($"[{color.ToMarkup()}]{icon}[/]", "Required observer", DescribeRequirement(requirement).EscapeMarkup());
        }

        if (data.ObserverRequirements.Count == 0) return;
        if (data.ObserverRequirements.Count > maximumRows)
        {
            table.AddRow("·", "Required observers", WatchOverflowDetail(data.ObserverRequirements.Count - maximumRows, "requirements"));
        }
        table.AddRow(string.Empty, "Required observers", $"[{OutputFormatter.Muted.ToMarkup()}]state only; not proof of event freshness or delivery[/]");
    }

    static void RenderFindings(DiagnoseData data)
    {
        foreach (var finding in data.Findings)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]![/]  {finding.EventStore.EscapeMarkup()}/{finding.Namespace.EscapeMarkup()}: {finding.Check.EscapeMarkup()}: {finding.Detail.EscapeMarkup()}");
        }

        if (ShowScopeTails(data))
        {
            foreach (var scope in data.Scopes)
            {
                var icon = CheckCompleted(scope, "Event sequence") ? string.Empty : $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]  ";
                AnsiConsole.MarkupLine($"  {icon}{scope.EventStore.EscapeMarkup()}/{scope.Namespace.EscapeMarkup()}: event sequence tail: {ScopeTailDetail(scope)}");
            }
        }
    }

    static void RenderIncompleteChecks(DiagnoseData data)
    {
        if (data.ChecksComplete)
        {
            return;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Danger.ToMarkup()}]Could not check:[/]");
        foreach (var check in data.ChecksCouldNotRun)
        {
            AnsiConsole.MarkupLine($"    {DescribeCheckFailure(check).EscapeMarkup()}");
        }
    }
}
