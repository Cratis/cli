// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.for_InitCommand.when_generating_chronicle_md;

/// <summary>
/// The generated document is read by AI agents as ground truth, so a command written without
/// its branch is worse than no guidance at all — the agent runs it and gets "Unknown command".
/// Every Chronicle sub-branch lives under `cratis chronicle`, and this catches any that lose it.
/// </summary>
public partial class and_referencing_chronicle_commands : Specification
{
    static readonly string[] _chronicleBranches =
    [
        "event-stores", "namespaces", "event-types", "events", "observers", "subscriptions",
        "failed-partitions", "recommendations", "jobs", "identities", "projections",
        "read-models", "users", "applications", "diagnose", "workbench"
    ];

    string _content;
    List<string> _unbranched;

    [GeneratedRegex("cratis (?<rest>[a-z][a-z-]*)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InvocationRegex { get; }

    void Because()
    {
        _content = ChronicleDocGenerator.Generate();
        _unbranched = [.. InvocationRegex
            .Matches(_content)
            .Select(m => m.Groups["rest"].Value)
            .Where(rest => _chronicleBranches.Contains(rest, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)];
    }

    [Fact] void should_not_reference_any_chronicle_command_without_its_branch() => _unbranched.ShouldBeEmpty();
    [Fact] void should_not_reference_a_config_command_group() => _content.ShouldNotContain("cratis config ");
    [Fact] void should_point_at_diagnose_for_connection_problems() => _content.ShouldContain("cratis chronicle diagnose");
}
