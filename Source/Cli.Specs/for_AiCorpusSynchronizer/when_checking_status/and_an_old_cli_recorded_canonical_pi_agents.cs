// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_an_old_cli_recorded_canonical_pi_agents : given.an_old_pi_installation
{
    AiStatus _status = null!;
    void Because() => _status = AiCorpusSynchronizer.Status(_project);

    [Fact] void should_accept_the_recorded_layout() => _status.ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_preserve_the_canonical_link() => AgentsTarget.ShouldEqual("../.cratis/ai/agents");
}
