// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Writes an observation report in the requested output format.
/// </summary>
/// <remarks>
/// Machine formats write Screenplay's own result JSON; the human view prints the same finding statements and limits, so
/// both carry identical findings.
/// </remarks>
public static class ObservationReportOutput
{
    /// <summary>
    /// Writes the report.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="report">The report.</param>
    /// <param name="output">The writer to write to.</param>
    public static void Write(string format, ObservationReport report, TextWriter output)
    {
        var json = ObservationReportJson.Write(report);
        switch (format)
        {
            case OutputFormats.Json:
                output.WriteLine(JsonNode.Parse(json)!.ToJsonString(new() { WriteIndented = true }));
                break;
            case OutputFormats.JsonCompact or OutputFormats.JsonQuiet:
                output.WriteLine(json);
                break;
            case OutputFormats.Quiet:
                output.WriteLine(Verdict(report));
                break;
            default:
                foreach (var line in Lines(report))
                {
                    output.WriteLine(line);
                }

                break;
        }
    }

    /// <summary>
    /// Gets the verdict word for a report.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns><c language="text">drift</c>, <c language="text">no-drift</c> or <c language="text">inconclusive</c>.</returns>
    public static string Verdict(ObservationReport report) => report.HasDrift switch
    {
        true => "drift",
        false => "no-drift",
        null => "inconclusive"
    };

    /// <summary>
    /// Gets the human-readable lines of a report.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>The lines.</returns>
    public static IEnumerable<string> Lines(ObservationReport report)
    {
        var coverage = report.Coverage;
        var sequence = coverage.Sequence;
        yield return $"Observation comparison: {Verdict(report)} ({(report.Complete ? "complete" : "incomplete")})";
        yield return $"Scope: event store '{coverage.EventStore}', namespace '{coverage.Namespace}', sequence '{sequence.SequenceId}'";
        yield return sequence.CapturedTail is null
            ? "Coverage: the sequence held no events"
            : $"Coverage: examined {Range(sequence)} of captured tail {sequence.CapturedTail} ({(sequence.Complete ? "full" : "partial")})";
        yield return $"Events: {coverage.AppendedEvents} appended, {coverage.AttributedEvents} attributed, {coverage.UnattributedEvents} unattributed";
        foreach (var finding in report.Findings)
        {
            yield return $"[{finding.Outcome}] {finding.Statement}";
        }

        foreach (var statement in report.NotEstablished)
        {
            yield return $"Not established: {statement}";
        }
    }

    static string Range(ObservedSequenceCoverage sequence) =>
        sequence.FirstExamined is null ? "no events" : $"{sequence.FirstExamined}..{sequence.LastExamined}";
}
