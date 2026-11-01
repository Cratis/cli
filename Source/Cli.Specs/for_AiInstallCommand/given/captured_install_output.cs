// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiInstallCommand.given;

[Collection(CliSpecsCollection.Name)]
public class captured_install_output : Specification
{
    protected SyncResult _result = new([], [], Warnings: ["Existing warning"]);
    protected string _output = string.Empty;
    protected int _exitCode;

    protected void Write(string format, IReadOnlyList<string>? profiles = null)
    {
        using var output = new StringWriter();
        var previousOutput = Console.Out;
        try
        {
            Console.SetOut(output);
            _exitCode = AiInstallCommand.Write(_result, format, profiles: profiles);
            _output = output.ToString();
        }
        finally
        {
            Console.SetOut(previousOutput);
        }
    }

    protected string[] Warnings()
    {
        using var document = JsonDocument.Parse(_output);
        return [.. document.RootElement.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString()!)];
    }
}
