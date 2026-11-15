// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions;

public class when_the_model_names_no_extension : given.an_admission
{
    string _folder;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cratis-no-extension-").FullName;
        File.WriteAllText(Path.Combine(_folder, "model.play"), "concept Money : String\n");
    }

    Task Because() => Admit(_folder, true);

    void Destroy() => Directory.Delete(_folder, true);

    [Fact] void should_let_the_run_start() => Admitted.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_have_no_workspace() => Admitted.Workspace.ShouldBeNull();
}
