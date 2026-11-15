// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions.given;

/// <summary>
/// A run's admission of the extensions of a project, with the console captured.
/// </summary>
public class an_admission : Extensions.given.a_restored_extension_project
{
    TextWriter _previousOutput;
    TextWriter _previousError;
    StringWriter _output;
    StringWriter _error;

    protected RunExtensionAdmitted Admitted { get; private set; }

    protected string Output => $"{_output}{_error}";

    protected async Task Admit(string modelFolder, bool folderTarget)
    {
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        Console.SetOut(_output = new());
        Console.SetError(_error = new());
        try
        {
            Admitted = await RunExtensions.Admit(new RunSettings(), modelFolder, folderTarget, OutputFormats.JsonCompact, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(_previousOutput);
            Console.SetError(_previousError);
        }
    }
}
