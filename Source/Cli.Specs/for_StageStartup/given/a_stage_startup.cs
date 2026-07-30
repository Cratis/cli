// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageStartup.given;

public class a_stage_startup : Specification
{
    protected StageStartup _startup;

    void Establish() => _startup = new("cratis/stage:latest");
}
