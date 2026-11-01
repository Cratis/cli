// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_table_output_and_no_llm : given.an_interpret_command
{
    void Establish()
    {
        _settings.Output = OutputFormats.Table;
        _settings.NoLlm = true;
    }

    Task Because() => Interpret();

    [Fact] void should_include_none_and_its_source_in_the_result() => _output.ShouldContain("Language model: none; source: --no-llm. Interpreting with heuristics only.");
}
