// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.given;

/// <summary>
/// Runs command admission against real temporary inputs with Docker unavailable on PATH.
/// </summary>
public class a_run_command : Specification
{
    protected string _folder;
    protected RunSettings _settings;
    protected StringWriter _error;

    string _previousDirectory;
    string? _previousPath;
    TextWriter _previousError;

    void Establish()
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        _previousPath = Environment.GetEnvironmentVariable("PATH");
        _previousError = Console.Error;
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        Directory.SetCurrentDirectory(_folder);
        _folder = Directory.GetCurrentDirectory();
        Environment.SetEnvironmentVariable("PATH", _folder);
        _error = new StringWriter();
        Console.SetError(_error);
        _settings = new RunSettings { Path = _folder, Output = OutputFormats.JsonCompact };
    }

    /// <summary>
    /// Executes the run command through its established command interface.
    /// </summary>
    /// <returns>The exit code.</returns>
    protected Task<int> Execute() =>
        ((ICommand<RunSettings>)new RunCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "run", null),
            _settings,
            CancellationToken.None);

    void Destroy()
    {
        Console.SetError(_previousError);
        Environment.SetEnvironmentVariable("PATH", _previousPath);
        Directory.SetCurrentDirectory(_previousDirectory);
        _error.Dispose();
        Directory.Delete(_folder, true);
    }
}
