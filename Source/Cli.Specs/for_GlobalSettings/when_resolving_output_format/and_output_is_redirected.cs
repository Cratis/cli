// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GlobalSettings.when_resolving_output_format;

[Collection(CliSpecsCollection.Name)]
public sealed class and_output_is_redirected : Specification, IDisposable
{
    static readonly string[] _aiAgentEnvVars = ["CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "WINDSURF_SESSION_ID", "TERM_PROGRAM"];
    readonly Dictionary<string, string?> _savedEnvVars = [];

    string _previousNoColor;
    GlobalSettings _settings;
    string _result;

    void Establish()
    {
        _previousNoColor = Environment.GetEnvironmentVariable("NO_COLOR");
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        foreach (var key in _aiAgentEnvVars)
        {
            _savedEnvVars[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }
        _settings = new GlobalSettings { Output = OutputFormats.Auto };
    }

    void Because() => _result = _settings.ResolveOutputFormat();

    [Fact] void should_return_json_when_stdout_is_redirected() => _result.ShouldEqual(OutputFormats.Json);

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        Environment.SetEnvironmentVariable("NO_COLOR", _previousNoColor);
        foreach (var (key, value) in _savedEnvVars)
            Environment.SetEnvironmentVariable(key, value);
    }
}
