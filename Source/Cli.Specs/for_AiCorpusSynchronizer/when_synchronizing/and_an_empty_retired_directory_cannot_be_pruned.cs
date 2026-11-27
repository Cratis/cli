// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_empty_retired_directory_cannot_be_pruned : given.an_installation_with_agents_and_prompts
{
    void Establish() => RetireAgentsPromptsAndTheScanHook();

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    void should_finish_updating_the_installation(Type errorType)
    {
        var directory = Path.Combine(AiProjectPaths.PhysicalRoot(_project), ".pi", "prompts");
        var failed = false;
        var operations = new AiFileOperations(false)
        {
            ObserveDestination = (operation, path) =>
            {
                if (operation != "delete" || path != directory) return;
                failed = true;
                throw (Exception)Activator.CreateInstance(errorType)!;
            }
        };

        var result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses, new AiCorpusExecution(false, operations));

        failed.ShouldBeTrue();
        result.Conflicts.ShouldBeEmpty();
        Directory.Exists(directory).ShouldBeTrue();
        AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
    }
}
