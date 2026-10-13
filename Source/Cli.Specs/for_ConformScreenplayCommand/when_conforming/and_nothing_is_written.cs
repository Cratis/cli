// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_nothing_is_written : given.a_conform_command
{
    async Task Because() => await Execute();
    [Fact] void should_not_write_a_generated_model() => Directory.GetFiles(_folder).Order(StringComparer.Ordinal).ShouldEqual(new[] { _model, _project }.Order(StringComparer.Ordinal));
    [Fact] void should_not_modify_the_authored_model() => File.ReadAllText(_model).ShouldEqual(Source);
    [Fact] void should_not_modify_the_project() => File.ReadAllText(_project).ShouldEqual("<Project />");
}
