// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_completeness_cli_process : Specification
{
    protected int _exitCode;
    protected string _output;
    protected string _error;
    protected string _document;
    protected string _format = "json-compact";
    string _folder;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _document = Path.Combine(_folder, "MyApp.play");
        File.WriteAllText(_document, "module M\n  feature F\n    slice StateView View\n      event Unconsumed\n        id Uuid\n      screen One\n      screen Two\n");
    }

    protected async Task Run(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.Environment[UpdateChecker.DisableEnvVar] = "1";
        start.Environment["NO_COLOR"] = "1";
        start.ArgumentList.Add(typeof(ValidateScreenplayCommand).Assembly.Location);
        foreach (var argument in new[] { "screenplay", "validate", _document, "-o", _format }.Concat(args))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        await process.WaitForExitAsync(deadline.Token);
        _exitCode = process.ExitCode;
        _output = await output;
        _error = await error;
    }

    void Destroy() => Directory.Delete(_folder, true);
}
