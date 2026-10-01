// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    static bool CheckCompleted(DiagnoseData data, string check) => !data.ChecksCouldNotRun.Any(x => x.Check == check);

    static string CheckDetail(DiagnoseData data, string check, string completedDetail, bool reasonsAbove = false) => CheckCompleted(data, check)
        ? completedDetail
        : $"[{OutputFormatter.Danger.ToMarkup()}]could not check (see reasons {(reasonsAbove ? "above" : "below")})[/]";

    static string DescribeCheckFailure(DiagnoseCheckFailure check) =>
        $"{check.EventStore ?? "server"}/{check.Namespace ?? "all"}: {check.Check}: {check.Reason}";

    static void RenderFindings(DiagnoseData data)
    {
        foreach (var finding in data.Findings)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]![/]  {finding.EventStore.EscapeMarkup()}/{finding.Namespace.EscapeMarkup()}: {finding.Check.EscapeMarkup()}: {finding.Detail.EscapeMarkup()}");
        }

        if (data.Scopes.Count > 1)
        {
            foreach (var scope in data.Scopes)
            {
                AnsiConsole.MarkupLine($"  {scope.EventStore.EscapeMarkup()}/{scope.Namespace.EscapeMarkup()}: event sequence tail: {scope.EventSequenceTail?.ToString() ?? "unavailable"}");
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
