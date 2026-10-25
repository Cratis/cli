// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.given;

public class a_model_with_sample_data : a_run_command
{
    protected string _path;
    protected string _source;
    protected string _capturedSeed;

    void Establish()
    {
        _source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shell", "model", "application.play"));
        _path = Path.Combine(_folder, "application.play");
        _capturedSeed = Path.Combine(_tools, "seed.play");
        File.WriteAllText(_path, _source);
        if (!OperatingSystem.IsWindows())
        {
            File.WriteAllText(Path.Combine(_tools, "docker"),
                $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\nfor arg in \"$@\"; do\ncase \"$arg\" in *:/eventmodel/cratis-sample-data.play:ro) seed=${{arg%:/eventmodel/cratis-sample-data.play:ro}}; /bin/cat \"$seed\" > '{_capturedSeed}';; esac\ndone\nexit 1\n");
        }
    }
}
