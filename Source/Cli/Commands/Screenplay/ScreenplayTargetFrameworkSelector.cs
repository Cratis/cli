// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayTargetFrameworkSelector
{
    internal static ScreenplayTargetFrameworkSelection Select(
        IEnumerable<string> projectNames,
        string? requestedFramework,
        string? location = null)
    {
        var selected = new List<string>();
        var diagnostics = new List<ScreenplayDiagnostic>();
        var requested = string.IsNullOrWhiteSpace(requestedFramework) ? null : requestedFramework;

        foreach (var group in projectNames
                     .GroupBy(ScreenplayProjectSelection.WithoutTargetFramework, StringComparer.Ordinal)
                     .OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var variants = group.Order(StringComparer.Ordinal).ToArray();
            if (variants.Length == 1)
            {
                selected.Add(variants[0]);
                continue;
            }

            var frameworks = variants
                .Select(TargetFrameworkOf)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ThenBy(_ => _, StringComparer.Ordinal)
                .ToArray();
            var available = string.Join(", ", frameworks);

            if (requested is null)
            {
                diagnostics.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Error,
                    ScreenplayDiagnosticCodes.AmbiguousTargetFramework,
                    $"Project '{group.Key}' targets multiple frameworks: {available}. Pass --framework <TFM> to select one",
                    location));
                continue;
            }

            var matching = variants.FirstOrDefault(
                variant => string.Equals(TargetFrameworkOf(variant), requested, StringComparison.OrdinalIgnoreCase));
            if (matching is null)
            {
                diagnostics.Add(new ScreenplayDiagnostic(
                    ScreenplayDiagnosticSeverity.Error,
                    ScreenplayDiagnosticCodes.UnavailableTargetFramework,
                    $"Project '{group.Key}' does not target requested framework '{requested}'. Available target frameworks: {available}",
                    location));
                continue;
            }

            selected.Add(matching);
        }

        return new ScreenplayTargetFrameworkSelection(selected, diagnostics);
    }

    static string? TargetFrameworkOf(string projectName)
    {
        if (!projectName.EndsWith(')'))
        {
            return null;
        }

        var openingParenthesis = projectName.LastIndexOf('(');
        return openingParenthesis > 0 && openingParenthesis < projectName.Length - 2
            ? projectName[(openingParenthesis + 1)..^1]
            : null;
    }
}

internal sealed record ScreenplayTargetFrameworkSelection(
    IReadOnlyList<string> ProjectNames,
    IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    internal bool IsSuccessful => Diagnostics.Count == 0;
}
