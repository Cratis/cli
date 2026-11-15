// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_the_link_changes_before_deletion : given.an_ownership_plan
{
    Exception? _error;
    bool _swapped;
    string _path = null!;

    void Establish() => _path = Path.Combine(AiProjectPaths.PhysicalRoot(_project), ".pi", "skills");

    void Because()
    {
        var operations = new AiFileOperations(false)
        {
            ObserveDestination = (kind, path) =>
            {
                if (_swapped) return;
                if (kind != "delete" || path != _path) return;
                DeleteLink(_path);
                Directory.CreateSymbolicLink(_path, _corpus);
                _swapped = true;
            }
        };
        _error = Catch.Exception(() => AiCorpusSynchronizer.Uninstall(_project, false, false, operations));
    }

    [Fact] void should_exercise_the_mutation_boundary() => _swapped.ShouldBeTrue();
    [Fact] void should_reject_changed_ownership() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_leave_the_changed_link() => new DirectoryInfo(_path).LinkTarget.ShouldEqual(_corpus);
}
