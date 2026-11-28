// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildProcess;

[Collection(CliSpecsCollection.Name)]
public class when_launching_a_workload : given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_filter_credentials_after_applying_overrides()
    {
        var result = await BuildProcess.Run("/bin/sh", ["-c", Check], Folder, Overrides);
        result.ExitCode.ShouldEqual(0);
    }
}
