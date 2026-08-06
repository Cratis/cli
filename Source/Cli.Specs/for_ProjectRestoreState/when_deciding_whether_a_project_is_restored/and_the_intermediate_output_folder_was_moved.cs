// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_deciding_whether_a_project_is_restored;

/// <summary>
/// The artifacts output layout of the SDK moves the intermediate output folder out of the project folder, so the
/// assets file is nowhere near the <c>obj</c> beside the project and only the assembly says where it went.
/// </summary>
public class and_the_intermediate_output_folder_was_moved : given.a_project_folder
{
    string _assembly;
    bool _result;

    void Establish()
    {
        var intermediate = Path.Combine(_folder, "artifacts", "obj", "MyApp");
        Restore(intermediate);
        _assembly = Path.Combine(intermediate, "debug", "MyApp.dll");
    }

    void Because() => _result = ProjectRestoreState.IsRestored(_project, _assembly);

    [Fact] void should_take_the_project_for_restored() => _result.ShouldBeTrue();
}
