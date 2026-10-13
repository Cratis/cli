// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Bridges PATH rendering's temporary identities to the persisted model-root baseline check.
/// </summary>
/// <param name="root">The source root owning .screenplay metadata.</param>
/// <param name="run">The read-only persisted-identity check.</param>
internal sealed class ModelRootReleaseBaseline(string root, Func<string, ReleaseBaselineOutcome> run) : IRenderReleaseBaseline
{
    internal ModelRootReleaseBaseline(string root) : this(root, ReleaseBaselineCommands.Check)
    {
    }

    /// <inheritdoc/>
    public ReleaseBaselineAssessment Assess(SemanticCompilation compilation)
    {
        var revision = compilation.Model.Revision;
        var path = Path.Combine(root, ".screenplay", ReleaseBaselineStore.FileName);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return ReleaseBaselineAssessment.NotSupplied;
        }

        if (!File.Exists(Path.Combine(root, ".screenplay", "identities.json")))
        {
            return new(ReleaseBaselineAssessmentStatus.Unavailable, revision, [], "PATH renders need persisted .screenplay/identities.json; run cratis screenplay baseline check");
        }

        try
        {
            var outcome = run(root);

            // PATH documents use temporary identities, so Stage cannot reconstruct their persisted workspace.
            // The bridge checks the root read-only and deliberately stamps the rendered revision. The root's
            // sources may change between these two reads; this does not replace the baseline CI gate.
            if (outcome.ExitCode == 0)
            {
                return new(ReleaseBaselineAssessmentStatus.Checked, revision, []);
            }

            if (outcome.ExitCode == 1)
            {
                return Violations(path, revision, outcome);
            }

            return new(
                outcome.FailureKind == "UnreadableReleaseBaseline" ? ReleaseBaselineAssessmentStatus.Unreadable : ReleaseBaselineAssessmentStatus.Unavailable,
                revision,
                [],
                outcome.Reason ?? outcome.FailureKind);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidSemanticContract or ScreenplayWorkspaceStateConflict or ScreenplayWorkspaceMetadataConflict)
        {
            return new(ReleaseBaselineAssessmentStatus.Unavailable, revision, [], exception.Message);
        }
    }

    static ReleaseBaselineAssessment Violations(string path, SemanticRevision revision, ReleaseBaselineOutcome outcome)
    {
        var read = ReleaseBaselineFile.ReadForMapping(path);
        if (read.Status != "valid")
        {
            return new(ReleaseBaselineAssessmentStatus.Unreadable, revision, [], read.Reason);
        }

        var entries = read.Document!.Entries.ToDictionary(entry => (entry.Event, entry.Revision));
        var violations = new List<StageReleaseBaselineViolation>();
        foreach (var violation in outcome.Violations)
        {
            if (!entries.TryGetValue((violation.Event, violation.Revision), out var entry))
            {
                return new(ReleaseBaselineAssessmentStatus.Unavailable, revision, [], "The release baseline changed after its check; an admitted revision could not be mapped.");
            }

            violations.Add(new(violation.Kind, EventContractId.Parse(violation.Event), violation.Revision, entry.EventType, entry.LastAddress, entry.AdmittedIn, violation.Path));
        }

        return new(ReleaseBaselineAssessmentStatus.Checked, revision, [.. violations]);
    }
}
