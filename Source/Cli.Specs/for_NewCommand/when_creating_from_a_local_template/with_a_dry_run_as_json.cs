// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_NewCommand.when_creating_from_a_local_template;

[Collection(CliSpecsCollection.Name)]
public class with_a_dry_run_as_json : given.a_local_template
{
    int _exitCode;
    JsonElement _result;

    async Task Because()
    {
        _exitCode = await New("sample", "--language", "csharp", "-n", "Demo", "-o", _output, "--dry-run", "--format", "json", "--", "--Greeting", "Hi");
        _result = JsonElement.Parse(_console.ToString());
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_name_the_template() => _result.GetProperty("template").GetString().ShouldEqual("sample");
    [Fact] void should_name_the_creation() => _result.GetProperty("name").GetString().ShouldEqual("Demo");
    [Fact] void should_report_the_output() => _result.GetProperty("output").GetString().ShouldEqual(Path.GetFullPath(_output));
    [Fact] void should_report_a_dry_run() => _result.GetProperty("dryRun").GetBoolean().ShouldBeTrue();
    [Fact] void should_list_the_files_relative_to_the_output() => _result.GetProperty("files")[0].GetString().ShouldEqual("Demo.txt");
    [Fact] void should_report_the_post_action_as_skipped() => _result.GetProperty("postActions")[0].GetProperty("outcome").GetString().ShouldEqual("skipped");
    [Fact] void should_describe_the_post_action() => _result.GetProperty("postActions")[0].GetProperty("description").GetString().ShouldEqual("Follow up");
    [Fact] void should_skip_the_ai_update() => _result.GetProperty("aiUpdate").GetProperty("status").GetString().ShouldEqual("skipped");
    [Fact] void should_write_nothing() => Directory.Exists(_output).ShouldBeFalse();
}
