// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironments;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_managing_environments : given.a_temp_project
{
    IReadOnlyList<string> _afterCreate;
    IReadOnlyList<string> _afterDelete;
    string _selectionAfterDelete;
    bool _ignoreChangedTwice;
    Exception _duplicate;

    void Because()
    {
        var passphrase = PassphraseSources.Fixed("p");
        ProjectEnvironment.Create(_environments, "production", null, passphrase, FastKdf);
        ProjectEnvironment.Create(_environments, "development", null, passphrase, FastKdf);
        _duplicate = Catch.Exception(() => ProjectEnvironment.Create(_environments, "production", null, passphrase, FastKdf));
        _afterCreate = _environments.List();
        _environments.Select("production");
        _environments.EnsureLocalIgnored();
        _ignoreChangedTwice = _environments.EnsureLocalIgnored();
        _environments.Delete("production");
        _afterDelete = _environments.List();
        _selectionAfterDelete = _environments.Resolve(null).Name;
    }

    [Fact] void should_list_sorted_names() => _afterCreate.ShouldContainOnly("development", "production");
    [Fact] void should_remove_the_file() => _afterDelete.ShouldContainOnly("development");
    [Fact] void should_clear_a_selection_of_the_removed_environment() => _selectionAfterDelete.ShouldEqual("development");
    [Fact] void should_refuse_a_duplicate() => _duplicate.ShouldBeOfExactType<EnvironmentException>();
    [Fact] void should_add_the_ignore_entry_once() => _ignoreChangedTwice.ShouldBeFalse();
    [Fact] void should_refuse_unsafe_names() => Catch.Exception(() => _environments.PathFor("../x")).ShouldBeOfExactType<InvalidEnvironmentNameException>();
}
