// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Environments.for_EnvCommands;

[Collection(CliSpecsCollection.Name)]
public class when_creating_selecting_and_listing : given.an_environment_project
{
    int _initDefault;
    int _initProduction;
    int _duplicate;
    int _selectMissing;
    int _select;
    JsonElement _listed;
    JsonElement _listedWithOption;
    string _gitignore = string.Empty;

    async Task Because()
    {
        _initDefault = await Init();
        _initProduction = await Init("production");
        _duplicate = await Init("production");
        _selectMissing = await Run(new EnvSelectCommand(), new EnvNameSettings { Name = "nope" });
        _select = await Run(new EnvSelectCommand(), new EnvNameSettings { Name = "production" });
        await Run(new EnvListCommand(), new EnvironmentSettings());
        _listed = JsonElement.Parse(_output.ToString()).Clone();
        await Run(new EnvListCommand(), new EnvironmentSettings { EnvironmentShort = "development" });
        _listedWithOption = JsonElement.Parse(_output.ToString()).Clone();
        _gitignore = await File.ReadAllTextAsync(Path.Combine(_root, ".gitignore"));
    }

    static string Selected(JsonElement list) => list.EnumerateArray().Single(entry => entry.GetProperty("selected").GetBoolean()).GetProperty("name").GetString()!;

    [Fact] void should_create_the_default_environment_without_a_name() => File.Exists(Path.Combine(_root, ".cratis", "environments", "development.yaml")).ShouldBeTrue();
    [Fact] void should_create_a_named_environment() => _initProduction.ShouldEqual(ExitCodes.Success);
    [Fact] void should_succeed_for_the_default() => _initDefault.ShouldEqual(ExitCodes.Success);
    [Fact] void should_refuse_a_duplicate() => _duplicate.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_refuse_selecting_a_missing_environment() => _selectMissing.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_select() => _select.ShouldEqual(ExitCodes.Success);
    [Fact] void should_mark_the_selected_environment() => Selected(_listed).ShouldEqual("production");
    [Fact] void should_let_the_option_override_the_selection() => Selected(_listedWithOption).ShouldEqual("development");
    [Fact] void should_keep_the_selection_out_of_version_control() => _gitignore.ShouldContain(".cratis/local/");
}
