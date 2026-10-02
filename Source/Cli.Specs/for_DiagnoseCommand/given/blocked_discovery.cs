// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class blocked_discovery : healthy_services
{
    protected bool TextRowsAreUnavailable() => new[] { "Observers", "Quarantined observers", "Failed partitions", "Recommendations", "Event sequence" }
        .All(label => _outputs[OutputFormats.Table].Split(Environment.NewLine).Single(line => line.Contains($"{label,-21}  ", StringComparison.Ordinal)).TrimStart().StartsWith('✗'));

    protected bool WatchRowsAreUnavailable() => new[] { "Observers", "Quarantined observers", "Failed partitions", "Recommendations", "Event sequence tail" }
        .All(label => WatchIcons(label).Single().Text == "✗" && WatchIcons(label).Single().Style.Foreground == OutputFormatter.Danger);

    protected int BlockedJsonQueries(string format) => JsonDocument.Parse(_outputs[format]).RootElement.GetProperty("checksCouldNotRun").EnumerateArray()
        .Count(check => check.GetProperty("reason").GetString()!.StartsWith("skipped:", StringComparison.Ordinal) && check.GetProperty("check").GetString() != "Namespaces");

    protected int BlockedPlainQueries() => _outputs[OutputFormats.Plain].Split(Environment.NewLine)
        .Count(line => line.StartsWith("could_not_check=", StringComparison.Ordinal) && line.Contains("reason=\"skipped:", StringComparison.Ordinal) && !line.StartsWith("could_not_check=Namespaces ", StringComparison.Ordinal));
}
