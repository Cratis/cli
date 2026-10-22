// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_stage_refused_model : a_completeness_cli_process
{
    void Establish() => File.WriteAllText(_document, string.Join('\n', [
        "concept ProjectId : Uuid",
        "module Projects",
        "  feature Registration",
        "    slice StateChange RegisterProject",
        "      command RegisterProject",
        "        projectId ProjectId identifier",
        "        produces ProjectRegistered",
        "          projectId = projectId",
        "      event ProjectRegistered",
        "        projectId ProjectId"
    ]));
}
