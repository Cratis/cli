// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironments;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_resolving_the_selection : a_temp_project
{
    SelectedEnvironment _default;
    SelectedEnvironment _fromFile;
    SelectedEnvironment _fromVariable;
    SelectedEnvironment _fromOption;

    void Because()
    {
        _default = _environments.Resolve(null);
        _environments.Select("staging");
        _fromFile = _environments.Resolve(null);
        Environment.SetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable, "from-variable");
        _fromVariable = _environments.Resolve(null);
        _fromOption = _environments.Resolve("from-option");
    }

    [Fact] void should_default_to_development() => _default.ShouldEqual(new SelectedEnvironment("development", SelectionSource.Default));
    [Fact] void should_use_the_selection_file() => _fromFile.ShouldEqual(new SelectedEnvironment("staging", SelectionSource.SelectionFile));
    [Fact] void should_let_the_variable_override_the_file() => _fromVariable.ShouldEqual(new SelectedEnvironment("from-variable", SelectionSource.EnvironmentVariable));
    [Fact] void should_let_the_option_override_the_variable() => _fromOption.ShouldEqual(new SelectedEnvironment("from-option", SelectionSource.Option));
    [Fact] void should_store_the_selection_outside_the_environments_folder() => _environments.SelectionFile.ShouldContain(Path.Combine(".cratis", "local"));
    [Fact] void should_ignore_the_local_folder() => File.ReadAllText(Path.Combine(_root, ".gitignore")).ShouldContain(ProjectEnvironments.LocalIgnoreEntry);
}
