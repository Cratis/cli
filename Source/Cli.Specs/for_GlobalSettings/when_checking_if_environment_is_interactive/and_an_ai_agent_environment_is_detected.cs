// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GlobalSettings.when_checking_if_environment_is_interactive;

[Collection(CliSpecsCollection.Name)]
public class and_an_ai_agent_environment_is_detected : Specification
{
    string? _originalValue;
    bool _result;

    void Establish()
    {
        _originalValue = Environment.GetEnvironmentVariable("CLAUDECODE");
        Environment.SetEnvironmentVariable("CLAUDECODE", "1");
    }

    void Because() => _result = GlobalSettings.IsInteractiveEnvironment(true, true);

    void Destroy() => Environment.SetEnvironmentVariable("CLAUDECODE", _originalValue);

    [Fact] void should_not_be_interactive() => _result.ShouldBeFalse();
}
