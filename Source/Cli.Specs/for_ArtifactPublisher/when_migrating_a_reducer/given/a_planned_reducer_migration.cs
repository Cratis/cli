// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_migrating_a_reducer.given;

public class a_planned_reducer_migration : for_RenderCommand.given.a_classic_reducer_destination
{
    private protected ScreenplayRenderPlan _migration = null!;

    async Task Establish()
    {
        _migration = await new ScreenplayPlanning().Plan(
            new(_folder, "MyApp", "cratis") { Destination = _destination, MigrateReducers = [ReadModel] },
            CancellationToken.None);
        Assert.True(_migration.Success, string.Join("; ", _migration.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    private protected ArtifactPublicationRequest MigrationRequest =>
        new(_migration.Artifacts!, _destination, false) { SemanticAddresses = _migration.SemanticAddresses };

    private protected sealed class InterruptAfterOperations(int operations) : IArtifactPublicationObserver
    {
        int _applied;

        public void OnCheckpoint(ArtifactPublicationCheckpoint checkpoint)
        {
            if (checkpoint == ArtifactPublicationCheckpoint.OperationApplied && ++_applied == operations)
            {
                throw new SimulatedInterruption();
            }
        }
    }

    private protected sealed class SimulatedInterruption : Exception;
}
