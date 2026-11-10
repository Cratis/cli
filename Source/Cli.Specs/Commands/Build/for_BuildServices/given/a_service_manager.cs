// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices.given;

public class a_service_manager : Specification
{
    protected string _configHome;
    protected SystemdBuildService _platform;
    protected BuildServices _services;
    protected List<string> _ran;
    protected Dictionary<string, BuildProcessResult> _answers;
    protected BuildServiceDefinition _definition;

    void Establish()
    {
        _configHome = Path.Combine(Path.GetTempPath(), "cratis-build-service-specs", Guid.NewGuid().ToString("N"));
        _platform = new(_configHome);
        _ran = [];
        _answers = [];
        _services = new(_platform, (step, _) =>
        {
            _ran.Add(step.ToString());
            return Task.FromResult(_answers.TryGetValue(step.ToString(), out var answer) ? answer : new BuildProcessResult(0, string.Empty, string.Empty));
        });
        _definition = new("svc", "/usr/bin/cratis", ["build", "run"], "/src/shop", "/usr/bin");
    }

    void Destroy()
    {
        if (Directory.Exists(_configHome)) Directory.Delete(_configHome, recursive: true);
    }
}
