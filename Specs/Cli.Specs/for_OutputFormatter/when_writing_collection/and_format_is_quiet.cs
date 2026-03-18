// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_OutputFormatter.when_writing_collection;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_quiet : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var data = new[] { new { Name = "Alice", Age = 30 }, new { Name = "Bob", Age = 25 } };
        OutputFormatter.Write(OutputFormats.Quiet, data, ["Name", "Age"], item => [item.Name, item.Age.ToString()]);

        _output = writer.ToString();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    [Fact] void should_contain_first_item_identifier() => _output.ShouldContain("Alice");
    [Fact] void should_contain_second_item_identifier() => _output.ShouldContain("Bob");
    [Fact] void should_not_contain_header() => _output.ShouldNotContain("Name");
    [Fact] void should_not_contain_age_values() => _output.ShouldNotContain("30");
    [Fact] void should_have_one_item_per_line() => _output.Trim().Split('\n').Length.ShouldEqual(2);
}
