// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    static bool CheckCompleted(DiagnoseData data, string check) => !data.ChecksCouldNotRun.Any(x => x.Check == check);

    static string CheckDetail(DiagnoseData data, string check, string completedDetail, bool reasonsAbove = false) => CheckCompleted(data, check)
        ? completedDetail
        : $"[{OutputFormatter.Danger.ToMarkup()}]could not check (see reasons {(reasonsAbove ? "above" : "below")})[/]";

    static bool ShowScopeTails(DiagnoseData data) => data.Scopes.Count > 1 || (data.Scopes.Count > 0 && !data.ChecksComplete);

    static string ScopeTailDetail(DiagnoseData scope) => CheckCompleted(scope, "Event sequence")
        ? scope.EventSequenceTail?.ToString() ?? "empty"
        : $"[{OutputFormatter.Danger.ToMarkup()}]could not check[/]";

    static string DescribeCheckFailure(DiagnoseCheckFailure check) =>
        $"{check.EventStore ?? "server"}/{check.Namespace ?? "all"}: {check.Check}: {check.Reason}";

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
