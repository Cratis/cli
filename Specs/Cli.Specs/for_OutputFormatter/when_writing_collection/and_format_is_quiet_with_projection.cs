// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_OutputFormatter.when_writing_collection;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_quiet_with_projection : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var data = new[] { new { Name = "Alice", Age = 30 }, new { Name = "Bob", Age = 25 } };
        OutputFormatter.Write(
            OutputFormats.Quiet,
            data,
            ["Name", "Age"],
            item => [item.Name, item.Age.ToString()],
            quietProjection: item => $"{item.Name}:{item.Age}");

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_use_custom_projection() => _output.ShouldContain("Alice:30");
    [Fact] void should_contain_second_item() => _output.ShouldContain("Bob:25");
}
