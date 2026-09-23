// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Templates;

/// <summary>
/// The outcome of the AI-content sync that finishes every creation.
/// </summary>
/// <param name="Status">One of updated, skipped or failed.</param>
/// <param name="Actions">The actions taken, empty when none.</param>
/// <param name="Detail">Human readable detail — actions taken, the skip reason, or the error.</param>
public record CreatedProjectAiUpdateResult(string Status, IReadOnlyList<string> Actions, string Detail);

/// <summary>
/// Finishes a created project: changes into the created folder — the working directory for
/// everything that follows — and synchronizes the project's configured Cratis AI content there,
/// exactly as <c language="csharp">cratis ai update</c> would. Projects without a
/// <c language="csharp">.cratis/ai.json</c> skip gracefully; a failure is reported and never
/// discards the scaffold.
/// </summary>
public static class CreatedProjectAiUpdate
{
    const string ConfigurationPath = ".cratis/ai.json";

    /// <summary>
    /// Runs the finishing step against a created project.
    /// </summary>
    /// <param name="outputRoot">The folder the template was created in.</param>
    /// <returns>The sync outcome.</returns>
    public static CreatedProjectAiUpdateResult Run(string outputRoot)
    {
        // Everything after creation happens inside the created project.
        Environment.CurrentDirectory = outputRoot;

        if (!File.Exists(Path.Combine(outputRoot, ConfigurationPath)))
        {
            return new CreatedProjectAiUpdateResult(
                "skipped",
                [],
                $"no {ConfigurationPath} — the template carries no Cratis AI configuration.");
        }

        try
        {
            var configuration = AiCorpusSynchronizer.Status(outputRoot).Configuration;
            var source = Environment.GetEnvironmentVariable("CRATIS_AI_SOURCE") ?? AiCorpusSource.Download();
            var result = AiCorpusSynchronizer.Synchronize(outputRoot, source, configuration);
            if (result.Conflicts.Count > 0)
            {
                return new CreatedProjectAiUpdateResult(
                    "conflicts",
                    result.Actions,
                    $"Cratis-managed AI files were modified: {string.Join(", ", result.Conflicts)}. No files were changed.");
            }

            return new CreatedProjectAiUpdateResult(
                "updated",
                result.Actions,
                $"synchronized {result.Actions.Count} Cratis AI file(s).");
        }
        catch (Exception error)
        {
            return new CreatedProjectAiUpdateResult(
                "failed",
                [],
                $"running 'cratis ai update' in the created project failed: {error.Message}");
        }
    }
}
