// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_StartPrologueCommand.when_writing_configuration;

public class with_json_output : Specification
{
    string _folder;
    string _path;
    PrologueWizardInput _input;
    PrologueConfiguration _configuration;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _path = Path.Combine(_folder, "cratis-prologue.json");
        _input = new(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./my captures"));
    }

    async Task Because()
    {
        await StartPrologueCommand.WriteConfiguration(_input, _path);
        _configuration = PrologueConfigurationFile.Read(await File.ReadAllTextAsync(_path));
    }

    [Fact] void should_keep_the_host_directory_in_the_saved_configuration() => _configuration.Prologue.Output.Json.Directory.ShouldEqual("./my captures");
    [Fact] void should_resolve_the_host_captures_without_an_explicit_path() => CaptureFolders.Resolve(null, _configuration, _path, "/elsewhere").ShouldEqual(Path.Combine(_folder, "my captures"));

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
