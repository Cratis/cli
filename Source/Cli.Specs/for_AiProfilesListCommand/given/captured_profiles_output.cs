// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.given;

[Collection(CliSpecsCollection.Name)]
public class captured_profiles_output : Specification
{
    protected AiProfile[] _profiles =
    [
        new("cratis/documentation", "public", [], []),
        new("cratis/engineering/csharp", "engineering", ["csharp"], ["cratis/documentation"])
    ];
    protected string _output = string.Empty;
    protected int _exitCode;

    protected void Write(string format)
    {
        using var output = new StringWriter();
        var previousOutput = Console.Out;
        try
        {
            Console.SetOut(output);
            _exitCode = AiProfilesListCommand.Write(format, _profiles);
            _output = output.ToString();
        }
        finally
        {
            Console.SetOut(previousOutput);
        }
    }
}
