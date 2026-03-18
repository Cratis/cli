// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_OutputFormatter.when_writing_message;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_quiet : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        OutputFormatter.WriteMessage(OutputFormats.Quiet, "Operation succeeded");

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_produce_no_output() => _output.ShouldBeEmpty();
}
