// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating.and_generation_reports_an_error;

[Collection(CliSpecsCollection.Name)]
public class and_machine_output_is_requested : given.a_generation_reporting_an_error
{
    TextWriter _previousError;
    StringWriter _capturedError;
    string _result;

    void Establish()
    {
        _previousError = Console.Error;
        _capturedError = new StringWriter();
        Console.SetError(_capturedError);
    }

    async Task Because()
    {
        await Execute();
        _result = _capturedError.ToString();
    }

    [Fact] void should_write_one_json_payload() => JsonDocument.Parse(_result).RootElement.GetProperty("diagnostics").GetArrayLength().ShouldEqual(2);

    void Destroy()
    {
        Console.SetError(_previousError);
        _capturedError.Dispose();
    }
}
