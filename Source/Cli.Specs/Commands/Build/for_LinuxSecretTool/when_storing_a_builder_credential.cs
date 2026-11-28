// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.Commands.Build.for_LinuxSecretTool;

[Collection(CliSpecsCollection.Name)]
public class when_storing_a_builder_credential : given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_receive_only_the_intended_stdin_payload()
    {
        var tool = Script("secret-tool", Check + " && test \"$(cat)\" = test-payload");
        await new LinuxDirectSecrets(tool).Write("test-key", "test-payload", CancellationToken.None);
    }
}
