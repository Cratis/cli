// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed class AiTestRunner(IReadOnlyList<IAiTestHarness> harnesses, IAiTestProcess process, AiTestRedactor redactor)
{
    internal static AiConfiguration Configuration(string project, string corpus, bool corpusMode)
    {
        if (corpusMode && !File.Exists(Path.Combine(project, ".cratis", "ai.json"))) return AiCorpusSynchronizer.Available(corpus);
        var status = AiCorpusSynchronizer.Status(project);
        if (!corpusMode && !File.Exists(Path.Combine(project, ".cratis", "ai.manifest.json"))) throw new InvalidOperationException("No AI installation manifest exists. Run 'cratis ai install' first or supply --source.");
        if (!corpusMode && status.ModifiedFiles.Count > 0) throw new InvalidOperationException($"Cratis-managed AI files were modified: {string.Join(", ", status.ModifiedFiles)}. Restore the installation or supply --source.");
        return status.Configuration;
    }

    internal async Task<AiTestReport> Run(string project, string corpus, AiTestSettings settings, CancellationToken cancellationToken)
    {
        var configuration = Configuration(project, corpus, settings.Source is not null);
        var requested = RequestedHarnesses(settings.Harnesses);
        var selection = settings.Source is null ? configuration : AiCorpusSynchronizer.Available(corpus);
        var fixtures = AiTestFixture.Load(settings.Fixtures ?? Path.Combine(corpus, "Evaluations", "conformance"), selection, settings.Fixture);
        var results = new List<AiTestResult>();
        var session = new AiTestSession(process, redactor, new(configuration, corpus, settings, [.. harnesses.Select(adapter => adapter.Id)]));
        foreach (var fixture in fixtures)
        {
            foreach (var harness in SelectHarnesses(configuration, requested, fixture.Fixture.Harnesses, settings.InstallOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await session.Run(fixture, settings.InstallOnly ? null : Adapter(harness), harness, cancellationToken));
            }
        }
        return AiTestReport.Create(results);
    }

    static string[]? RequestedHarnesses(string? requested)
    {
        if (requested is null) return null;
        var selection = requested.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (selection.Length == 0) throw new InvalidOperationException("--harnesses requires at least one harness.");
        return [.. selection.Distinct(StringComparer.Ordinal)];
    }

    IAiTestHarness? Adapter(string id) => harnesses.FirstOrDefault(adapter => adapter.Id == id);

    string[] SelectHarnesses(AiConfiguration configuration, string[]? requested, string[] declared, bool installOnly)
    {
        if (requested is not null) return [.. requested.Where(id => declared.Length == 0 || declared.Contains(id, StringComparer.Ordinal))];
        if (installOnly) return declared.Length > 0 ? [.. declared.Distinct(StringComparer.Ordinal)] : [.. configuration.Harnesses];
        return [.. configuration.Harnesses.Where(id => Adapter(id) is not null && (declared.Length == 0 || declared.Contains(id, StringComparer.Ordinal)))];
    }
}
