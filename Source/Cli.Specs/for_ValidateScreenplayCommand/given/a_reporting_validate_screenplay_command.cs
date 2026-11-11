// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_reporting_validate_screenplay_command : a_validate_screenplay_command
{
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
        _output.Dispose();
        _error.Dispose();
    }
}
